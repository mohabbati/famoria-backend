namespace Famoria.Domain.Entities;

public class FamoriaUser : AuditableEntity
{
    public FamoriaUser()
    {
        // Parameterless constructor for Cosmos DB deserialization
    }

    public FamoriaUser(string id)
    {
        Id = id;
    }

    public FamoriaUser(string id, string email, string provider, string providerSub, IList<string> familyIds)
    {
        Id = id;
        Email = email;
        Provider = provider;
        ProviderSub = providerSub;
        FamilyIds = familyIds ?? [];
    }

    public string Email { get; set; } = default!;
    public string GivenName { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Provider { get; set; } = default!;
    public string ProviderSub { get; set; } = default!;
    public IList<string> FamilyIds { get; set; } = [];
}
