namespace Famoria.Application.Interfaces;

public interface IAesCryptoService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
