namespace Famoria.Application.Interfaces;

public interface IJwtService
{
    string Sign(string subject, string email, string? familyId = null);
}
