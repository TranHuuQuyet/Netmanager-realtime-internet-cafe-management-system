using System;
using System.Text;

namespace Shared.Networking;

public static class NetworkProtocol
{
    /* mã hoá theo chuẩn UTF8
    false không thêm bất kì kí tự nào trước đoạn mã hoá*/
    public static readonly Encoding TextEncoding = new UTF8Encoding(false);
    //tạo định danh cho server
    public const string ServerSource = "server";
    // Kiểm tra message hợp lệ trước khi gửi 
    public static string ValidateOutgoingMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))// kiểm tra message null hoặc space 
        {
            throw new ArgumentException("Network message cannot be empty.", nameof(message));
        }

        if (message.Contains('\n') || message.Contains('\r'))// kiểm tra message có chứa \n hay \r 
        {
            throw new ArgumentException("Network message must be one JSON object per line.", nameof(message));
        }

        return message;
    }
}
