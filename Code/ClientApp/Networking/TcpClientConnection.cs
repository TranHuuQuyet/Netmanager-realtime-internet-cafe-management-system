    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Shared.Networking;

    namespace ClientApp.Networking;

    public class TcpClientConnection : IDisposable
    {/*Giới hạn chỉ 1 thread gửi dữ liệu tại một thời điểm
new(1,1) — tham số đầu: số thread được vào ngay (1), 
tham số hai: tối đa bao nhiêu thread được vào (1). 
Tức là chỉ 1 thread ghi stream tại một thời điểm*/
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private readonly object _stateLock = new();
/*Bộ 4 object đại diện cho kết nối TCP hiện tại
Tất cả đều nullable (?) vì lúc đầu chưa connect, chúng là null
Sau khi ConnectAsync() xong mới có giá trị. Khi disconnect thì reset về null lại.*/
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private StreamReader? _reader;
        private StreamWriter? _writer;
        //một “cờ huỷ” được truyền vào các hàm async để chúng biết khi nào cần dừng.
        private CancellationTokenSource? _connectionTokenSource;
        private Task? _receiveLoopTask;
        private Task? _reconnectTask;
        private string? _host;
        private int _port;
        private bool _autoReconnect;
        private bool _isDisconnected = true;

        public bool IsConnected
        {
            get
            {
                lock (_stateLock)
                {
                    return !_isDisconnected;
                }
            }
        }

        public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(3);

        public event Action? Connected;
        public event Action<string>? MessageReceived;
        public event Action? Disconnected;
        public event Action<Exception>? ReconnectFailed;

        public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
        {// lưu biến vào biến khác để reconnect
            _host = host;
            _port = port;
//gọi hàm ConnectOnceAsync và chờ nó làm xong 
            await ConnectOnceAsync(host, port, cancellationToken);
        }

        public void EnableAutoReconnect()
        {
            _autoReconnect = true;
        }

        public void DisableAutoReconnect()
        {
            _autoReconnect = false;
        }

        public async Task SendAsync(string message, CancellationToken cancellationToken = default)
        {
            string outgoingMessage = NetworkProtocol.ValidateOutgoingMessage(message);

            await _sendLock.WaitAsync(cancellationToken);

            try
            {
                StreamWriter? writer;

                lock (_stateLock)
                {
                    if (_isDisconnected)
                    {
                        return;
                    }

                    writer = _writer;
                }

                if (writer == null)
                {
                    return;
                }

                await writer.WriteLineAsync(outgoingMessage.AsMemory(), cancellationToken);
            }
            catch (IOException)
            {
                HandleConnectionLost();
            }
            catch (SocketException)
            {
                HandleConnectionLost();
            }
            catch (ObjectDisposedException)
            {
                HandleConnectionLost();
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private async Task ConnectOnceAsync(string host, int port, CancellationToken cancellationToken)
        {// nếu đang kết nối rồi thì không kết nối lại
            if (IsConnected)
            {
                return;
            }

            TcpClient tcpClient = new();

            try
            {// thực hiện TCP handshake với server, chờ xong mới xuống
                await tcpClient.ConnectAsync(host, port, cancellationToken);

                NetworkStream stream = tcpClient.GetStream();
                // reader tự convert bytes → text cho mình
                // thay vì đọc bytes:  [72][101][108][108][111]
                // chỉ cần gọi:        reader.ReadLineAsync() → "Hello"
                StreamReader reader = new(stream, NetworkProtocol.TextEncoding);
                // writer tự convert text → bytes cho mình
                // ghi bytes:  [72][101][108][108][111]
                // chỉ cần gọi:        writer.WriteLineAsync("Hello")           
                StreamWriter writer = new(stream, NetworkProtocol.TextEncoding)
                {// AutoFlush = true — ghi xuống stream ngay, không đợi buffer đầy
                    AutoFlush = true
                };// CancellationTokenSource bên trong thực chất chứa sẵn 1 CancellationToken
                CancellationTokenSource connectionTokenSource = new();

                lock (_stateLock)
                {
                    DisposeCurrentConnection();
// copy → field — mọi hàm dùng được
                    _tcpClient = tcpClient;
                    _stream = stream;
                    _reader = reader;
                    _writer = writer;
                    _connectionTokenSource = connectionTokenSource;
                    // kết nối thành công   
                    _isDisconnected = false;
                }
// bắn event ra ngoài báo kết nối thành công
// bắn SAU lock vì không nên giữ lock lâu
// app bên ngoài nhận event, biết đã connect xong
                Connected?.Invoke();
// dòng này: gọi hàm → hàm chạy ngầm
// ĐỒNG THỜI xuống dòng tiếp theo luôn
                _receiveLoopTask = RunReceiveLoopAsync(connectionTokenSource.Token);
            }
            catch
            {// nếu có lỗi giải phóng tcpClient
                tcpClient.Dispose();
                //ném exception lên tầng trên.
                throw;
            }
        }

        private async Task RunReceiveLoopAsync(CancellationToken cancellationToken)
        {
            try
            {// chạy hàm ReceiveLoopAsync và quan sát cờ cênllationToken
                await ReceiveLoopAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLIENT] Receive loop stopped unexpectedly: {ex.Message}");
            }
        }
//vòng lặp nhận dữ liệu
        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            try
            {//Loop chạy liên tục cho tới khi token bị huỷ.
                while (!cancellationToken.IsCancellationRequested)
                {/*Khai báo biến reader.
                    StreamReader → object đọc text từ stream
                    ? → cho phép null*/
                    StreamReader? reader;

                    lock (_stateLock)
                    {//kiểm tra client đã connect chưa
                        if (_isDisconnected)
                        {
                            break;
                        }
                        /*Copy ra biến local tránh 
                        + async
                        + race condition*/ 
                        reader = _reader;
                    }
//Nếu không có reader thì thoát vòng lặp. Không có reader thì không thể đọc dữ liệu từ server.
                    if (reader == null)
                    {
                        break;
                    }
// Đọc 1 dòng message từ server. Nó sẽ chờ tới khi server gửi dữ liệu có xuống dòng \n.
                    string? message = await reader.ReadLineAsync(cancellationToken);
// Nếu message == null nghĩa là server đã đóng kết nối hoặc stream kết thúc. Thoát loop.
                    if (message == null)
                    {
                        break;
                    }
//Nếu message rỗng hoặc toàn dấu cách thì bỏ qua, không xử lý. continue nghĩa là quay lại đầu vòng lặp để chờ message tiếp theo.
                    if (string.IsNullOrWhiteSpace(message))
                    {
                        Console.WriteLine("[CLIENT] Empty message from server.");
                        continue;
                    }

                    try
                    {//thông bán message
                        MessageReceived?.Invoke(message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CLIENT] Message handler failed: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (IOException)
            {
                Console.WriteLine("[CLIENT] Lost connection to server.");
            }
            catch (SocketException)
            {
                Console.WriteLine("[CLIENT] Socket error while receiving server message.");
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {//finally luôn chạy cuối cùng, dù loop dừng vì break, vì lỗi, hay vì cancel.
            //Nó dùng để xử lý mất kết nối: dọn tài nguyên, đổi trạng thái, báo client đã disconnect.
                HandleConnectionLost();
            }
        }

        private void HandleConnectionLost()
        {
            CloseConnection(allowReconnect: true);
        }

        public void Disconnect()
        {
            _autoReconnect = false;
            CloseConnection(allowReconnect: false);
        }

        private void CloseConnection(bool allowReconnect)
        {
            bool shouldNotify;

            lock (_stateLock)
            {
                if (_isDisconnected)
                {
                    return;
                }

                _isDisconnected = true;
                shouldNotify = true;
//đổi trạng thái huỷ (cancel state) của token thành true.
                _connectionTokenSource?.Cancel();
                DisposeCurrentConnection();
            }

            if (shouldNotify)
            {//shouldNotify là một “cờ” để quyết định có được phép bắn event Disconnected hay không.
                Disconnected?.Invoke();
            }

            if (allowReconnect)
            {
                StartReconnectLoop();
            }
        }

        private void StartReconnectLoop()
        {/*nếu _autoReconnect == false
                → không được tự reconnect → thoát
                nếu _host rỗng/null
                → không biết reconnect tới đâu → thoát*/
            if (!_autoReconnect || string.IsNullOrWhiteSpace(_host))
            {
                return;
            }

            lock (_stateLock)
            {/*_reconnectTask khác null
                và task đó chưa hoàn thành (IsCompleted == false)*/
                if (_reconnectTask is { IsCompleted: false })
                {
                    return;
                }

                _reconnectTask = ReconnectLoopAsync(_host, _port);
            }
        }

        private async Task ReconnectLoopAsync(string host, int port)
        {/*còn cho phép reconnect
        → tiếp tục thử
        nếu _autoReconnect = false
        → dừng loop.*/
            while (_autoReconnect)
            {
                try
                {//Đợi vài giây trước khi reconnect.
                    await Task.Delay(ReconnectDelay);

                    if (!_autoReconnect)
                    {
                        return;
                    }
//Thử kết nối lại server
                    await ConnectOnceAsync(host, port, CancellationToken.None);
                    return;
                }
                catch (Exception ex)
                {
                    ReconnectFailed?.Invoke(ex);
                }
            }
        }

        private void DisposeCurrentConnection()
        {
            _reader?.Dispose();
            _writer?.Dispose();
            _stream?.Dispose();
            _tcpClient?.Dispose();
            _connectionTokenSource?.Dispose();

            _reader = null;
            _writer = null;
            _stream = null;
            _tcpClient = null;
            _connectionTokenSource = null;
        }

        public void Dispose()
        {
            Disconnect();
            _sendLock.Dispose();
        }
    }
