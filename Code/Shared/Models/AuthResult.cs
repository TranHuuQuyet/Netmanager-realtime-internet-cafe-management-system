// Namespace chua cac model dung chung.
namespace Shared.Models;

// Ket qua xac thuc/dang nhap don gian.
public record AuthResult(bool Success, string? Token, string? ErrorCode, string? ErrorMessage);
