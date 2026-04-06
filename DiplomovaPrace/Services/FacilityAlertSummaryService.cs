using System.Collections.Generic;

namespace DiplomovaPrace.Services;

public sealed class FacilityAlertNodeSummaryItem
{
    public required string NodeKey { get; init; }
    public required string Label { get; init; }
    public required string FacilityName { get; init; }
    public required NodeDeviationSeverity Severity { get; init; }
}

public sealed class FacilityGlobalAlertSummary
{
    public int HighCount { get; init; }
    public int ElevatedCount { get; init; }
    public string ActiveFacilityName { get; init; } = string.Empty;
    public IReadOnlyList<FacilityAlertNodeSummaryItem> AlertedNodes { get; init; } = [];
    public DateTime UpdatedUtc { get; init; } = DateTime.UtcNow;
}

public sealed class FacilityAlertSummaryService
{
    private FacilityGlobalAlertSummary _current = new();

    public FacilityGlobalAlertSummary Current => _current;

    public event Action? OnChanged;

    public void Update(FacilityGlobalAlertSummary summary)
    {
        _current = summary;
        OnChanged?.Invoke();
    }
}
