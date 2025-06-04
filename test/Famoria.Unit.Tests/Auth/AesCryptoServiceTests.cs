using System;
using Famoria.Application.Services;
using FluentAssertions;
using Xunit;

namespace Famoria.Unit.Tests.Auth;

public class AesCryptoServiceTests
{
    private static readonly byte[] Key = new byte[32] {
        1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32
    };

    [Fact]
    public void Encrypt_Then_Decrypt_ShouldReturnOriginal()
    {
        var service = new AesCryptoService(Key);
        var original = "SensitiveData123!";
        var encrypted = service.Encrypt(original);
        encrypted.Should().NotBeNullOrWhiteSpace();
        encrypted.Should().NotBe(original);
        var decrypted = service.Decrypt(encrypted);
        decrypted.Should().Be(original);
    }

    [Fact]
    public void Ciphertext_ShouldNotContainPlaintext()
    {
        var service = new AesCryptoService(Key);
        var original = "SensitiveData123!";
        var encrypted = service.Encrypt(original);
        encrypted.Should().NotContain(original);
    }
}
