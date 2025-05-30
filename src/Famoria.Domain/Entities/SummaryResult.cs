namespace Famoria.Domain.Entities;

public class SummaryResult
{
    public required string Title { get; set; }
    public string[] KeyPoints { get; set; } = [];
    public string[] ActionItems { get; set; } = [];
    public string Language { get; set; } = "en";
    public string SourceTextHash { get; set; } = string.Empty;
    public string? ModelUsed { get; set; }
    public List<string>? Tags { get; set; }
}
