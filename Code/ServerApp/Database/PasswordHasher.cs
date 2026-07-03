using System.Security.Cryptography;
using ServerApp.Database.Models;

// Namespace cua tang Database.
namespace ServerApp.Database;

// Lop tien ich de bam mat khau.
// Hash mat khau giup khong can luu mat khau goc dang plain text.
public static class PasswordHasher
{
    // Do dai salt tinh bang byte.
    // Salt la du lieu ngau nhien them vao truoc khi hash de cung mat khau van ra hash khac nhau.
    private const int SaltSize = 16;

    // Do dai khoa/hash dau ra tinh bang byte.
    private const int KeySize = 32;

    // So vong lap PBKDF2.
    // So cang lon thi cang kho brute-force hon, nhung xu ly cung cham hon.
    private const int Iterations = 100_000;

    // Tao salt ngau nhien va hash cho mat khau dau vao.
    public static PasswordHash Hash(string password)
    {
        // Nem loi neu password null, rong hoac chi toan khoang trang.
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        // Tao salt ngau nhien moi cho moi lan hash.
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        // PBKDF2 bien password + salt thanh hash an toan hon so voi hash mot lan don gian.
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        // Luu salt va hash bang Base64 de de ghi vao database dang text.
        return new PasswordHash(
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }
}
