namespace NETManager.Shared.Models;

public record AuthResult(bool Success, string? Token, string? ErrorCode, string? ErrorMessage);
