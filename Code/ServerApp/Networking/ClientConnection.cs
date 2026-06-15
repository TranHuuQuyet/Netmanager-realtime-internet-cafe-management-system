/* async = keywork để có thể sử dụng await để bất đồng bộ 
await = = tạm dừng hàm tại đây cho tới khi operation hoàn thành,
nhưng không block thread
đại diện cho một thao tác bất đồng bộ (asynchronous)*/

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Shared.Networking;

namespace ServerApp.Networking;

// Đại diện cho một client TCP đang kết nối tới ServerApp.
//
// Nhiệm vụ:
// - Đọc từng dòng JSON từ NetworkStream.
// - Gửi từng dòng JSON về client.
// - Báo event MessageReceived khi client gửi packet.
// - Báo event Disconnected khi socket đóng hoặc lỗi.
//
// Lưu ý:
// - Class này không hiểu LOGIN/STATUS/ACK là gì.
// - Nó chỉ quản lý stream/socket; TcpJsonLineServer mới xử lý protocol.
public class ClientConnection : IDisposable
{
    public string ClientId { get; }

    private readonly TcpClient _tcpClient;
    private readonly NetworkStream _stream;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    // giới hạn thread được xử lý dữ liệu từ stream  
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly CancellationTokenSource _disconnectTokenSource = new();
    //quản lý viẹc các thread sử dụng biến isdisconnected 
    private readonly object _stateLock = new();
    // property 
    private bool _isDisconnected;
    private string? _sessionId;

    public bool IsDisconnected
    {
        get
        {
            lock (_stateLock)
            {
                return _isDisconnected;
            }
        }
    }

    public string? SessionId
    {
        get
        {
            lock (_stateLock)
            {
                return _sessionId;
            }
        }
    }
    /* event là một cơ chế chứa danh sách các delegate (hàm callback) 
    mà class khác có thể đăng ký vào để được thông báo khi một sự kiện xảy ra.*/
    // tạo event 
    // MessageReceived thông báo client vừa gửi message
    public event Action<ClientConnection, string>? MessageReceived;
    // Disconnected thông báo client vừa ngắt kết nối 
    public event Action<ClientConnection>? Disconnected;

    public ClientConnection(string clientId, TcpClient tcpClient)// Construtors
    {
        ClientId = clientId;

        _tcpClient = tcpClient;

        _stream = _tcpClient.GetStream();

        _reader = new StreamReader(_stream, NetworkProtocol.TextEncoding);

        _writer = new StreamWriter(_stream, NetworkProtocol.TextEncoding)
        {
            /* properity của StreamWrite dùng để đẩy dữ liệu từ buffer xuống stream 
            true = đẩy dữ liệu ngay khi ghi xong vao buffer          
            false = đẩy khi buffer đầy
            hoặc Flush()
            hoặc Close()
            hoặc Dispose()*/
            AutoFlush = true
        };
    }

    public bool TryBindSession(string sessionId)
    {
        // Bind sessionId vào connection hiện tại.
        //
        // Chỉ bind một lần để tránh một socket đổi session giữa chừng.
        // Nếu connection đã disconnect hoặc đã có sessionId thì trả false.
        if (string.IsNullOrWhiteSpace(sessionId))
            return false;

        lock (_stateLock)
        {
            if (_isDisconnected || _sessionId is not null)
                return false;

            _sessionId = sessionId.Trim();
            return true;
        }
    }

    // Gửi dữ liệu cho server
    public async Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        // kiểm tra input với điều kiện
        // Send flow:
        // - validate message đúng format JSON-line protocol
        // - dùng SemaphoreSlim để tránh nhiều thread ghi vào cùng stream một lúc
        // - nếu socket/stream lỗi thì disconnect connection
        //
        // Lỗi thường xảy ra khi:
        // - client mất mạng
        // - client đóng app trong lúc server đang gửi
        // - NetworkStream đã bị dispose
        string outgoingMessage = NetworkProtocol.ValidateOutgoingMessage(message);

        if (IsDisconnected)
            return;
        /*Chờ để vào SemaphoreSlim, đồng thời quan sát CancellationToken.*/
        await _sendLock.WaitAsync(cancellationToken);

        try
        {
            if (IsDisconnected)
                return;
            // AsMemory() == biến string thành ReadOnlyMemory<char> là một struct nhỏ dùng để mô tả vùng ký tự của string.
            await _writer.WriteLineAsync(outgoingMessage.AsMemory(), cancellationToken);
        }
        catch (IOException)
        {
            // Lỗi ghi stream.
            //
            // Thường xảy ra khi:
            // - client mất mạng
            // - client tắt app đột ngột
            // - socket bị đóng giữa lúc server đang WriteLineAsync
            Console.WriteLine($"[SERVER] Cannot send message to client: {ClientId}");
            Disconnect();
        }
        catch (ObjectDisposedException)
        {
            // Stream/socket đã bị dispose trước hoặc trong lúc send.
            Disconnect();
        }
        finally
        {
            _sendLock.Release();
        }
    }

    //vòng lặp liên tục nghe dữ liệu từ client gửi cho server 
    public async Task ReceiveLoopAsync(CancellationToken cancellationToken = default)
    {
        /* CreateLinkedTokenSource hàm gội nhiều token lại nếu 1 cái bị huỷ thì cái còn lại cũng vậy
        cancellationToken = tín hiệu huỷ từ bên ngoài truyền vào.
        _disconnectTokenSource.Token = tín hiệu huỷ nội bộ khi client connection bị disconnect.
*/
        // Receive flow:
        // - đọc từng dòng text cho tới khi gặp newline
        // - bỏ qua dòng rỗng
        // - bắn event MessageReceived để server xử lý packet
        // - nếu stream lỗi hoặc token bị hủy thì disconnect
        //
        // Hàm này chạy nền cho từng client connection.
        using CancellationTokenSource linkedTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disconnectTokenSource.Token);

        try
        {
            //linkedTokenSource.Token.IsCancellationRequested = check 1 trong 2 token được linked có bị huỷ ch
            while (!IsDisconnected && !linkedTokenSource.Token.IsCancellationRequested)
            {// Đưa đoạn byte được gửi về text và đọc tới khi thấy \n
                string? message = await _reader.ReadLineAsync(linkedTokenSource.Token);

                if (message == null)
                    break;

                if (string.IsNullOrWhiteSpace(message))
                {
                    Console.WriteLine($"[SERVER] Empty message from client: {ClientId}");
                    continue;
                }

                try
                {// sau khi đã đọc thì thông báo cho các hàm đã đăng kí nghe 
                 //  client vừa gửi message
                    MessageReceived?.Invoke(this, message);
                }
                // Bắt mọi lỗi xảy ra bên trong message handler.
                catch (Exception ex)
                {
                    Console.WriteLine($"[SERVER] Message handler failed for client {ClientId}: {ex.Message}");
                }
            }
        } // Xảy ra khi CancellationToken bị huỷ.
        catch (OperationCanceledException)
        {
            // Read loop bị hủy bởi cancellation token.
            //
            // Thường xảy ra khi:
            // - server đang Dispose/Stop
            // - Disconnect() đã hủy _disconnectTokenSource
            // - test chủ động đóng connection
        }// Lỗi đọc/ghi stream.
         //
         // Thường xảy ra khi:
         // - client mất mạng
         // - client tắt app đột ngột
         // - socket bị đóng giữa lúc đang read/write
        catch (IOException)
        {
            Console.WriteLine($"[SERVER] Client lost connection: {ClientId}");
        } // Lỗi trực tiếp từ tầng TCP/socket.
        catch (SocketException)
        {
            // Lỗi trực tiếp từ tầng TCP/socket.
            //
            // Thường xảy ra khi:
            // - connection bị reset
            // - network stack báo lỗi socket
            // - client đóng kết nối không theo flow bình thường
            Console.WriteLine($"[SERVER] Socket error from client: {ClientId}");
        }
        finally
        {
            Disconnect();
        }
    }

    public void Disconnect()
    {
        // Disconnect flow:
        // - đánh dấu disconnected dưới lock để chỉ chạy một lần
        // - hủy token để ReceiveLoopAsync dừng lại
        // - dispose reader/writer/stream/tcpClient
        // - emit Disconnected cho TcpJsonLineServer cleanup binding/session/pending command
        bool shouldNotify;

        lock (_stateLock)
        {
            if (_isDisconnected)
                return;

            _isDisconnected = true;
            shouldNotify = true;
        }

        _disconnectTokenSource.Cancel();

        _reader.Dispose();
        _writer.Dispose();
        _stream.Dispose();
        _tcpClient.Dispose();

        if (shouldNotify)
        {// thông báo client bị ngắt kết nối 
            Disconnected?.Invoke(this);
        }
    }

    public void Dispose()
    {
        Disconnect();
    }
}
