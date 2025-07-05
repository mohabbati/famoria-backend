namespace Famoria.Domain.Enums;

/// <summary>
/// Enumeration of detection status after matching recipients.
/// </summary>
public enum DetectionStatusType
{
    /// <summary>Recipients were successfully matched.</summary>
    Matched,
    /// <summary>No specific recipients detected.</summary>
    Undetected,
    /// <summary>Broadcast to all members.</summary>
    Broadcast
}
