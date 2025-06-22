using System.Security.Cryptography;

using Famoria.Application.Interfaces;

namespace Famoria.Application.Services;

public class AesCryptoService : IAesCryptoService
{
    private readonly byte[] _key;

    public AesCryptoService(byte[] key)
    {
        if (key == null || key.Length != 32)
            throw new ArgumentException("Key must be 32 bytes (256 bits) for AES-256.");
        _key = key;
    }

    public string Encrypt(string plainText)
    {
        if (plainText == null) throw new ArgumentNullException(nameof(plainText));
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        var iv = aes.IV;
        var encryptor = aes.CreateEncryptor(aes.Key, iv);
        using var ms = new System.IO.MemoryStream();
        ms.Write(iv, 0, iv.Length); // Prepend IV
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new System.IO.StreamWriter(cs))
        {
            sw.Write(plainText);
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (cipherText == null) throw new ArgumentNullException(nameof(cipherText));
        var encrypted = Convert.FromBase64String(cipherText);
        if (encrypted.Length < 16) throw new ArgumentException("Ciphertext too short.");
        using var aes = Aes.Create();
        aes.Key = _key;
        var iv = new byte[16];
        Array.Copy(encrypted, 0, iv, 0, iv.Length);
        aes.IV = iv;
        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new System.IO.MemoryStream(encrypted, 16, encrypted.Length - 16);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new System.IO.StreamReader(cs);
        return sr.ReadToEnd();
    }
}
// This implementation uses a random IV per encryption and expects a 256-bit key injected at runtime. Store and inject the key securely (e.g., from a key vault).
