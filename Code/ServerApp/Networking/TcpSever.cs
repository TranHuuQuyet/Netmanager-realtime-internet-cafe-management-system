using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
//đại diện trạng thái của một công việc bất đồng bộ đang hoặc sẽ chạy.
namespace ServerApp.Networking;

public class TcpServer
{
    private TcpListener? _listener;
    private Task? _acceptLoopTask;
    private volatile bool _isRunning;

    private readonly Dictionary<string, ClientConnection> _clients = new();
    /*lock tạo vùng chỉ cho 1 thread chạy, 
    / còn programmer dùng vùng đó để truy cập tài nguyên cần bảo vệ một cách an toàn.*/
    private readonly object _clientsLock = new();

    public event Action<string>? ClientConnected;
    public event Action<string>? ClientDisconnected;
    public event Action<string, string>? MessageReceived;

    public IReadOnlyCollection<string> ConnectedClientIds
    {
        get
        {
            lock (_clientsLock)
            {/*.ToList() — tạo List mới, copy toàn bộ reference vào
             không thay đổi dù _clients dict thay đổi sau đó_clients.Values          snapshot = ToList()
            ┌─────────────┐          ┌─────────────┐
            │ connA       │  copy →  │ connA       │
            │ connB       │  copy →  │ connB       │
            │ connC       │  copy →  │ connC       │
            └─────────────┘          └─────────────┘
            thay đổi                 không đổi
            theo thời gian           dù dict thay đổi*/
                return _clients.Keys.ToList();
            }
        }
    }

    public Task StartAsync(int port)
    {
        if (_isRunning)
        {//Task.CompletedTask đánh dấu task đó đã hoàn thành 
            return Task.CompletedTask;
        }

        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();

        _isRunning = true;

        Console.WriteLine($"[SERVER] Started on port {port}");
        /* đặt hàm RunAcceptLoopAsync vào Task
        ngay lập tức chạy RunAcceptLoopAsync()*/
        _acceptLoopTask = RunAcceptLoopAsync();

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;
// tạo một biến acceptLoopTask và để nó rỏ chung với _acceptLoopTask 
        Task? acceptLoopTask = _acceptLoopTask;

        _listener?.Stop();
// tạo biến clients kiểu list lưu trữ danh sách các client trong distionary 
        List<ClientConnection> clients;
//tạo ra một vùng khoá hành động, luồng nào muốn chạy thì phải chờ được cấp _clientLock
        lock (_clientsLock)
        {//copy toàn bộ values vào biến clients vừa tạo 
            clients = _clients.Values.ToList();
        }
// duyệt từng phần tử trong list vừa copy gán vào client
        foreach (ClientConnection client in clients)
        {// xoá client
            client.Disconnect();
        }

        lock (_clientsLock)
        {// xoá bảng copy đó 
            _clients.Clear();
        }
    //kiểm tra biến có đang trỏ tới object Task nào không 
        if (acceptLoopTask != null)
        {// chờ đợi object được acceptLoppTask trỏ đến hoàn thành 
        // trong khi quan sát biến cacellationToken
            await acceptLoopTask.WaitAsync(cancellationToken);
        }

        _acceptLoopTask = null;

        Console.WriteLine("[SERVER] Stopped");
    }
// Task được tạo ra để quản ý lỗi AcceptClientsAsyns()
    private async Task RunAcceptLoopAsync()
    {
        try
        {// AcceptClientsAsync chạy ngầm
    // RunAcceptLoopAsync DỪNG tại đây
    // chờ AcceptClientsAsync kết thúc hẳn
    // → catch bên dưới bắt được lỗi ✓
            await AcceptClientsAsync();
        }
        catch (Exception ex)
        {
            if (_isRunning)
            {
                Console.WriteLine($"[SERVER] Accept loop stopped unexpectedly: {ex.Message}");
            }
        }
    }
// vòng lập để server nhận client mới   
    private async Task AcceptClientsAsync()
    {// kiểm tra xem _listener có rỏ đến object TcpListen nào không 
        if (_listener == null)
        {
            throw new InvalidOperationException("Server has not been started.");
        }
// vòng lập xét điều kiện nếu server vẫn còn đang chạy
        while (_isRunning)
        {
            try
            {//chờ một client kết nối với server 
                TcpClient tcpClient = await _listener.AcceptTcpClientAsync();
// tạo ID ngẫu nhiên cho client
                string clientId = Guid.NewGuid().ToString();
// tạo một connection truyền vào 2 ham số 
                ClientConnection connection =
                    new ClientConnection(clientId, tcpClient);
// khi nhận được thông báo client gửi message thì chạy hàm OnClientMessageReceived 
// khi nhận được thông báo client bị ngắt kết nối thì chạy hàm OnClientDisconnected
                connection.MessageReceived += OnClientMessageReceived;
                connection.Disconnected += OnClientDisconnected;

                lock (_clientsLock)
                { // Lưu connection của client theo clientId
                    _clients[clientId] = connection;
                }

                Console.WriteLine($"[SERVER] Client connected: {clientId}");
//thông báo rằng client vừa connect vào server
                ClientConnected?.Invoke(clientId);
// chạy method RêciveLoopAsync
                _ = connection.ReceiveLoopAsync();
            }
            catch (SocketException)
            {
                if (_isRunning)
                {
                    throw;
                }
            }
            catch (ObjectDisposedException)
            {
                if (_isRunning)
                {
                    throw;
                }
            }
        }
    }

    public async Task SendAsync(string clientId, string message, CancellationToken cancellationToken = default)
    {
        ClientConnection? client;

        lock (_clientsLock)
        {//tra cứu trong dicto các key clientId và đưa ra value là object ClientConneciton
            _clients.TryGetValue(clientId, out client);
        }
// check điều kiện có client hay không 
        if (client == null)
        {
            Console.WriteLine($"[SERVER] Client not found: {clientId}");
            return;
        }
// gửi dữ liệu cho server và check cờ cancellationTo
        await client.SendAsync(message, cancellationToken);
    }
// Gửi cùng 1 message tới tất cả client đang kết nối
    public async Task BroadcastAsync(string message, CancellationToken cancellationToken = default)
    { // khai báo biến list chứa ClientConnection
        List<ClientConnection> clients;

        lock (_clientsLock)
        {
            clients = _clients.Values.ToList();
        }
 // duyệt từng client trong snapshot vừa copy
        foreach (ClientConnection client in clients)
        {
            await client.SendAsync(message, cancellationToken);
        }
    }

    public void RemoveClient(string clientId)
    {
        bool removed;

        lock (_clientsLock)
        {
            removed = _clients.Remove(clientId);
        }

        if (removed)
        {
            Console.WriteLine($"[SERVER] Client disconnected: {clientId}");
            ClientDisconnected?.Invoke(clientId);
        }
    }
// hàm dùng để thông báo ra giao diện người dùng clientID vừa gửi message
    private void OnClientMessageReceived(ClientConnection client, string message)
    {
        MessageReceived?.Invoke(client.ClientId, message);
    }
// hàm dùng để thông báo ra giao diện người dùng clientID vừa disconnected
    private void OnClientDisconnected(ClientConnection client)
    {
        RemoveClient(client.ClientId);
    }
}
