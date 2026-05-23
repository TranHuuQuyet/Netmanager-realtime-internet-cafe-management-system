using System.Security.Cryptography;
using ServerApp.Auth.Models;

namespace ServerApp.Auth.Services;

public static class PasswordHasher {
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public static PasswordHash Hash(string password) {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return new PasswordHash(
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public static bool Verify(string password, string saltBase64, string expectedHashBase64) {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(saltBase64) || string.IsNullOrWhiteSpace(expectedHashBase64)) {
            return false;
        }

        try {
            var salt = Convert.FromBase64String(saltBase64);
            var expectedHash = Convert.FromBase64String(expectedHashBase64);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException) {
            return false;
        }
    }
}
