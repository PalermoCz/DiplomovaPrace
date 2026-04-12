using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiplomovaPrace.Services;

public sealed class FacilityNodeEditorState
{
    public required string NodeKey { get; init; }
    public string? Label { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public string? Note { get; init; }
    public double? XHint { get; init; }
    public double? YHint { get; init; }
    public DateTime UpdatedUtc { get; init; } = DateTime.UtcNow;
}

public sealed class FacilitySavedWorkingSet
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string> NodeKeys { get; init; } = [];
    public FacilityNodeSemanticQuery? SemanticQuery { get; init; }
    public DateTime UpdatedUtc { get; init; } = DateTime.UtcNow;
}

public sealed class FacilityEditorStateService
{
    private const int CurrentSchemaVersion = 1;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _stateFilePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public FacilityEditorStateService(IWebHostEnvironment environment)
    {
        _stateFilePath = Path.Combine(environment.ContentRootPath, "facility-editor-state.json");
    }

    public async Task<IReadOnlyDictionary<string, FacilityNodeEditorState>> GetNodeStatesByKeyAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);

        try
        {
            var state = await LoadStateUnsafeAsync(ct);
            return state.NodeEdits
                .GroupBy(node => node.NodeKey, StringComparer.OrdinalIgnoreCase)
                .Select(group => NormalizeNodeState(group.Last()))
                .ToDictionary(node => node.NodeKey, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<FacilitySavedWorkingSet>> GetWorkingSetsAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);

        try
        {
            var state = await LoadStateUnsafeAsync(ct);
            return state.WorkingSets
                .GroupBy(workingSet => workingSet.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group => NormalizeWorkingSet(group.Last()))
                .OrderBy(workingSet => workingSet.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveNodeStatesAsync(IEnumerable<FacilityNodeEditorState> nodeStates, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);

        try
        {
            var state = await LoadStateUnsafeAsync(ct);
            var byNodeKey = state.NodeEdits
                .GroupBy(node => node.NodeKey, StringComparer.OrdinalIgnoreCase)
                .Select(group => NormalizeNodeState(group.Last()))
                .ToDictionary(node => node.NodeKey, StringComparer.OrdinalIgnoreCase);

            foreach (var nodeState in nodeStates)
            {
                var normalized = NormalizeNodeState(nodeState);
                if (!HasNodePayload(normalized))
                {
                    byNodeKey.Remove(normalized.NodeKey);
                    continue;
                }

                byNodeKey[normalized.NodeKey] = normalized;
            }

            state.SchemaVersion = CurrentSchemaVersion;
            state.NodeEdits = byNodeKey.Values
                .OrderBy(node => node.NodeKey, StringComparer.OrdinalIgnoreCase)
                .ToList();

            await PersistStateUnsafeAsync(state, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveWorkingSetAsync(FacilitySavedWorkingSet workingSet, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);

        try
        {
            var normalized = NormalizeWorkingSet(workingSet);
            var state = await LoadStateUnsafeAsync(ct);
            var existingIndex = state.WorkingSets.FindIndex(item =>
                item.Name.Equals(normalized.Name, StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
            {
                state.WorkingSets[existingIndex] = normalized;
            }
            else
            {
                state.WorkingSets.Add(normalized);
            }

            state.WorkingSets = state.WorkingSets
                .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.Last())
                .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            state.SchemaVersion = CurrentSchemaVersion;

            await PersistStateUnsafeAsync(state, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> DeleteWorkingSetAsync(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        await _gate.WaitAsync(ct);

        try
        {
            var state = await LoadStateUnsafeAsync(ct);
            var removed = state.WorkingSets.RemoveAll(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (removed <= 0)
            {
                return false;
            }

            state.SchemaVersion = CurrentSchemaVersion;
            await PersistStateUnsafeAsync(state, ct);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<FacilityEditorStateDocument> LoadStateUnsafeAsync(CancellationToken ct)
    {
        if (!File.Exists(_stateFilePath))
        {
            return new FacilityEditorStateDocument();
        }

        try
        {
            var raw = await File.ReadAllTextAsync(_stateFilePath, ct);
            var state = JsonSerializer.Deserialize<FacilityEditorStateDocument>(raw, JsonOptions);
            return state ?? new FacilityEditorStateDocument();
        }
        catch
        {
            return new FacilityEditorStateDocument();
        }
    }

    private async Task PersistStateUnsafeAsync(FacilityEditorStateDocument state, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_stateFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        state.SchemaVersion = CurrentSchemaVersion;
        var raw = JsonSerializer.Serialize(state, JsonOptions);
        var tempPath = _stateFilePath + ".tmp";

        await File.WriteAllTextAsync(tempPath, raw, ct);
        File.Move(tempPath, _stateFilePath, overwrite: true);
    }

    private static bool HasNodePayload(FacilityNodeEditorState nodeState)
    {
        return !string.IsNullOrWhiteSpace(nodeState.Label)
            || nodeState.XHint.HasValue
            || nodeState.YHint.HasValue
            || !string.IsNullOrWhiteSpace(nodeState.Note)
            || nodeState.Tags.Count > 0;
    }

    private static FacilityNodeEditorState NormalizeNodeState(FacilityNodeEditorState nodeState)
    {
        var nodeKey = nodeState.NodeKey?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nodeKey))
        {
            throw new InvalidOperationException("NodeKey musí být vyplněný.");
        }

        return new FacilityNodeEditorState
        {
            NodeKey = nodeKey,
            Label = NormalizeOptionalText(nodeState.Label),
            Tags = NormalizeTags(nodeState.Tags),
            Note = NormalizeOptionalText(nodeState.Note),
            XHint = NormalizeHint(nodeState.XHint),
            YHint = NormalizeHint(nodeState.YHint),
            UpdatedUtc = nodeState.UpdatedUtc == default ? DateTime.UtcNow : nodeState.UpdatedUtc
        };
    }

    private static FacilitySavedWorkingSet NormalizeWorkingSet(FacilitySavedWorkingSet workingSet)
    {
        var name = workingSet.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Název working setu musí být vyplněný.");
        }

        var normalizedSemanticQuery = NormalizeSemanticQuery(workingSet.SemanticQuery);

        return new FacilitySavedWorkingSet
        {
            Name = name,
            Description = NormalizeOptionalText(workingSet.Description),
            NodeKeys = workingSet.NodeKeys
                .Where(nodeKey => !string.IsNullOrWhiteSpace(nodeKey))
                .Select(nodeKey => nodeKey.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            SemanticQuery = normalizedSemanticQuery,
            UpdatedUtc = DateTime.UtcNow
        };
    }

    private static FacilityNodeSemanticQuery? NormalizeSemanticQuery(FacilityNodeSemanticQuery? semanticQuery)
    {
        if (semanticQuery is null)
        {
            return null;
        }

        return new FacilityNodeSemanticQuery
        {
            Role = semanticQuery.Role,
            SemanticType = semanticQuery.SemanticType,
            Tags = FacilityNodeSemantics.ParseTagQuery(string.Join(",", semanticQuery.Tags)),
            IncludeWeatherContext = semanticQuery.IncludeWeatherContext
        };
    }

    private static IReadOnlyList<string> NormalizeTags(IReadOnlyList<string> tags)
    {
        if (tags.Count == 0)
        {
            return [];
        }

        return tags
            .SelectMany(tag => FacilityNodeSemantics.ParseTagQuery(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? NormalizeOptionalText(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return raw.Trim();
    }

    private static double? NormalizeHint(double? hint)
    {
        if (!hint.HasValue)
        {
            return null;
        }

        return Math.Clamp(hint.Value, 0.0, 1.0);
    }

    private sealed class FacilityEditorStateDocument
    {
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public List<FacilityNodeEditorState> NodeEdits { get; set; } = [];
        public List<FacilitySavedWorkingSet> WorkingSets { get; set; } = [];
    }
}
