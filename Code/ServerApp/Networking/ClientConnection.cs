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

    // Gửi dữ liệu cho server
    public async Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        // kiểm tra input với điều kiện
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
            Console.WriteLine($"[SERVER] Cannot send message to client: {ClientId}");
            Disconnect();
        }
        catch (ObjectDisposedException)
        {
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
            Console.WriteLine($"[SERVER] Socket error from client: {ClientId}");
        }
        finally
        {
            Disconnect();
        }
    }

    public void Disconnect()
    {
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
