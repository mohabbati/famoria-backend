namespace Famoria.Domain.Enums;

/// <summary>
/// Enumeration of possible label categories.
/// </summary>
public enum LabelType
{
    /// <summary>Material-related emails (e.g., supply lists).</summary>
    Material,
    /// <summary>Event-related emails (e.g., calendar invites).</summary>
    Event,
    /// <summary>Reminder emails (actionable notifications).</summary>
    Reminder,
    /// <summary>Informational content (newsletters, updates).</summary>
    Information
}
