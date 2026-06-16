namespace ServerApp.Networking;

// Ghi lại một sự kiện mạng để UI/test có thể quan sát.
//
// Flow sử dụng:
// - TcpJsonLineServer tạo trace khi client connect/disconnect, gửi/nhận packet.
// - NetworkSmokeTest đọc trace để chứng minh packet thật đã đi qua TCP server.
// - UI có thể dùng trace để hiển thị log debug cho admin.
public sealed record NetworkTraceEntry(string Direction, string ClientId, string Message);
