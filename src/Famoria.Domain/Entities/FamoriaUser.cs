namespace Famoria.Domain.Entities;

public class FamoriaUser : AuditableEntity
{
    public FamoriaUser(string id)
    {
        Id = id;
    }

    public FamoriaUser(string id, string email, string provider, string externalSub, IList<string> familyIds)
    {
        Id = id;
        Email = email;
        Provider = provider;
        ExternalSub = externalSub;
        FamilyIds = familyIds ?? [];
    }

    public string Email { get; private init; } = default!;
    public string Provider { get; private init; } = default!;
    public string ExternalSub { get; private init; } = default!;
    public IList<string> FamilyIds { get; private init; } = [];
}
