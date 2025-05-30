namespace Famoria.Domain.Entities;

public class CalendarPayload : FamilyItemPayload
{
    public override FamilyItemSource Source => FamilyItemSource.Calendar;
    public required string Title { get; set; }
    public required DateTime StartTime { get; set; }
    public required DateTime EndTime { get; set; }
    public string? Location { get; set; }
}
