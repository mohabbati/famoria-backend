using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Famoria.Domain.Entities;

public class UserIntegrationConnection : AuditableEntity
{
    public required string FamilyId { get; init; }        // Partition key
    public required string UserId { get; init; }          // Who connected it
    public required string Provider { get; init; }        // e.g., "Google", "Microsoft"
    public required FamilyItemSource Source { get; init; }          // e.g., "Email", "Calendar", "Drive"
    public required string UserEmail { get; init; }       // Email address tied to the integration

    public string? AccessToken { get; set; }              // Store temporarily if needed
    public string? RefreshToken { get; set; }             // Optional — store securely

    public DateTime? TokenExpiresAtUtc { get; set; }      // Optional, helps with refresh logic
    public DateTime? LastFetchedAtUtc { get; set; }       // Tracking fetch progress

    public bool IsActive { get; set; } = true;            // Allow disabling integrations
}
