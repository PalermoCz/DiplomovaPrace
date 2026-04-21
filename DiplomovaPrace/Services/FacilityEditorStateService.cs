using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiplomovaPrace.Services;

public sealed class FacilityNodeEditorState
{
    public required string NodeKey { get; init; }
    public string? Label { get; init; }
    public string? NodeType { get; init; }
    public string? Zone { get; init; }
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

public sealed class FacilityStructureNode
{
    public required string NodeKey { get; init; }
    public required string Label { get; init; }
    public string? NodeType { get; init; }
    public string? Zone { get; init; }
    public string? MeterUrn { get; init; }
    public string? ParentNodeKey { get; init; }
    public double? XHint { get; init; }
    public double? YHint { get; init; }
    public DateTime UpdatedUtc { get; init; } = DateTime.UtcNow;
}

public sealed class FacilityStructureState
{
    public IReadOnlyList<FacilityStructureNode> AddedNodes { get; init; } = [];
    public IReadOnlySet<string> RemovedNodeKeys { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string?> ParentOverrides { get; init; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
}

public sealed class FacilityStructureEditResult
{
    public bool Success { get; init; }
    public required string Message { get; init; }

    public static FacilityStructureEditResult Ok(string message)
    {
        return new FacilityStructureEditResult
        {
            Success = true,
            Message = message
        };
    }

    public static FacilityStructureEditResult Fail(string message)
    {
        return new FacilityStructureEditResult
        {
            Success = false,
            Message = message
        };
    }
}

public sealed class FacilityEditorStateService
{
    private const int CurrentSchemaVersion = 2;

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

    /// <summary>
    /// Vrátí celý stav editoru jako raw JSON string pro snapshot/draft účely.
    /// Pokud soubor neexistuje, vrátí prázdný výchozí stav.
    /// </summary>
    public async Task<string> GetRawStateAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (!File.Exists(_stateFilePath))
                return JsonSerializer.Serialize(new FacilityEditorStateDocument(), JsonOptions);
            return await File.ReadAllTextAsync(_stateFilePath, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Obnoví stav editoru z raw JSON stringu (pro cancel/undo draft edit session).
    /// </summary>
    public async Task RestoreFromRawStateAsync(string rawJson, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var directory = Path.GetDirectoryName(_stateFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            var tempPath = _stateFilePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, rawJson, ct);
            File.Move(tempPath, _stateFilePath, overwrite: true);
        }
        finally
        {
            _gate.Release();
        }
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

    public async Task<FacilityStructureState> GetStructureStateAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);

        try
        {
            var state = await LoadStateUnsafeAsync(ct);
            return NormalizeStructureState(state.Structure);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<FacilityStructureEditResult> TryAddNodeAsync(
        FacilityStructureNode node,
        IReadOnlyDictionary<string, string?> currentParentByNodeKey,
        CancellationToken ct = default)
    {
        FacilityStructureNode normalizedNode;
        try
        {
            normalizedNode = NormalizeStructureNode(node);
        }
        catch (InvalidOperationException ex)
        {
            return FacilityStructureEditResult.Fail(ex.Message);
        }

        if (currentParentByNodeKey.ContainsKey(normalizedNode.NodeKey))
        {
            return FacilityStructureEditResult.Fail($"Node {normalizedNode.NodeKey} už existuje.");
        }

        if (FacilityNodeSemantics.IsWeatherContextNode(normalizedNode.NodeKey))
        {
            return FacilityStructureEditResult.Fail("Weather context node nelze přidat ani upravovat v schema editoru.");
        }

        if (!string.IsNullOrWhiteSpace(normalizedNode.ParentNodeKey)
            && !currentParentByNodeKey.ContainsKey(normalizedNode.ParentNodeKey))
        {
            return FacilityStructureEditResult.Fail($"Parent node {normalizedNode.ParentNodeKey} neexistuje.");
        }

        await _gate.WaitAsync(ct);

        try
        {
            var state = await LoadStateUnsafeAsync(ct);
            var normalizedStructure = NormalizeStructureStateMutable(state.Structure);

            normalizedStructure.RemovedNodeKeys.RemoveAll(nodeKey => nodeKey.Equals(normalizedNode.NodeKey, StringComparison.OrdinalIgnoreCase));
            normalizedStructure.ParentOverrides.Remove(normalizedNode.NodeKey);

            var existingIndex = normalizedStructure.AddedNodes.FindIndex(existing =>
                existing.NodeKey.Equals(normalizedNode.NodeKey, StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
            {
                normalizedStructure.AddedNodes[existingIndex] = normalizedNode;
            }
            else
            {
                normalizedStructure.AddedNodes.Add(normalizedNode);
            }

            normalizedStructure.Sort();
            state.Structure = normalizedStructure.ToDocument();
            state.SchemaVersion = CurrentSchemaVersion;

            await PersistStateUnsafeAsync(state, ct);
            return FacilityStructureEditResult.Ok($"Node {normalizedNode.NodeKey} byl přidán.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<FacilityStructureEditResult> TryReconnectNodeAsync(
        string nodeKey,
        string? newParentNodeKey,
        IReadOnlyDictionary<string, string?> currentParentByNodeKey,
        CancellationToken ct = default)
    {
        string normalizedNodeKey;
        try
        {
            normalizedNodeKey = NormalizeRequiredNodeKey(nodeKey);
        }
        catch (InvalidOperationException ex)
        {
            return FacilityStructureEditResult.Fail(ex.Message);
        }

        var normalizedParentKey = NormalizeOptionalNodeKey(newParentNodeKey);

        if (!currentParentByNodeKey.ContainsKey(normalizedNodeKey))
        {
            return FacilityStructureEditResult.Fail($"Node {normalizedNodeKey} neexistuje.");
        }

        if (FacilityNodeSemantics.IsWeatherContextNode(normalizedNodeKey))
        {
            return FacilityStructureEditResult.Fail("Weather context node nelze reconnectovat.");
        }

        if (!string.IsNullOrWhiteSpace(normalizedParentKey) && !currentParentByNodeKey.ContainsKey(normalizedParentKey))
        {
            return FacilityStructureEditResult.Fail($"Cílový parent {normalizedParentKey} neexistuje.");
        }

        if (!string.IsNullOrWhiteSpace(normalizedParentKey)
            && normalizedNodeKey.Equals(normalizedParentKey, StringComparison.OrdinalIgnoreCase))
        {
            return FacilityStructureEditResult.Fail("Node nelze připojit sám na sebe.");
        }

        if (!string.IsNullOrWhiteSpace(normalizedParentKey)
            && WouldCreateCycle(normalizedNodeKey, normalizedParentKey, currentParentByNodeKey))
        {
            return FacilityStructureEditResult.Fail("Reconnect by vytvořil cyklus v hierarchii.");
        }

        await _gate.WaitAsync(ct);

        try
        {
            var state = await LoadStateUnsafeAsync(ct);
            var normalizedStructure = NormalizeStructureStateMutable(state.Structure);

            var currentParent = NormalizeOptionalNodeKey(currentParentByNodeKey[normalizedNodeKey]);
            if (string.Equals(currentParent, normalizedParentKey, StringComparison.OrdinalIgnoreCase))
            {
                normalizedStructure.ParentOverrides.Remove(normalizedNodeKey);
            }
            else
            {
                normalizedStructure.ParentOverrides[normalizedNodeKey] = normalizedParentKey;
            }

            normalizedStructure.Sort();
            state.Structure = normalizedStructure.ToDocument();
            state.SchemaVersion = CurrentSchemaVersion;
            await PersistStateUnsafeAsync(state, ct);

            var parentText = normalizedParentKey ?? "root";
            return FacilityStructureEditResult.Ok($"Node {normalizedNodeKey} je nyní připojen na {parentText}.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<FacilityStructureEditResult> TryRemoveLeafNodeAsync(
        string nodeKey,
        IReadOnlyDictionary<string, string?> currentParentByNodeKey,
        CancellationToken ct = default)
    {
        string normalizedNodeKey;
        try
        {
            normalizedNodeKey = NormalizeRequiredNodeKey(nodeKey);
        }
        catch (InvalidOperationException ex)
        {
            return FacilityStructureEditResult.Fail(ex.Message);
        }

        if (!currentParentByNodeKey.ContainsKey(normalizedNodeKey))
        {
            return FacilityStructureEditResult.Fail($"Node {normalizedNodeKey} neexistuje.");
        }

        if (FacilityNodeSemantics.IsWeatherContextNode(normalizedNodeKey))
        {
            return FacilityStructureEditResult.Fail("Weather context node nelze odstranit.");
        }

        var hasChildren = currentParentByNodeKey.Any(pair =>
            !string.IsNullOrWhiteSpace(pair.Value)
            && pair.Value.Equals(normalizedNodeKey, StringComparison.OrdinalIgnoreCase));

        if (hasChildren)
        {
            return FacilityStructureEditResult.Fail("Node má potomky. V této verzi je možné mazat pouze leaf node.");
        }

        await _gate.WaitAsync(ct);

        try
        {
            var state = await LoadStateUnsafeAsync(ct);
            var normalizedStructure = NormalizeStructureStateMutable(state.Structure);

            var removedFromAdded = normalizedStructure.AddedNodes.RemoveAll(node =>
                node.NodeKey.Equals(normalizedNodeKey, StringComparison.OrdinalIgnoreCase));

            if (removedFromAdded == 0)
            {
                normalizedStructure.RemovedNodeKeys.Add(normalizedNodeKey);
            }

            normalizedStructure.ParentOverrides.Remove(normalizedNodeKey);
            state.NodeEdits.RemoveAll(node => node.NodeKey.Equals(normalizedNodeKey, StringComparison.OrdinalIgnoreCase));

            normalizedStructure.Sort();
            state.Structure = normalizedStructure.ToDocument();
            state.SchemaVersion = CurrentSchemaVersion;
            await PersistStateUnsafeAsync(state, ct);

            return FacilityStructureEditResult.Ok($"Leaf node {normalizedNodeKey} byl odstraněn.");
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

    private static FacilityStructureState NormalizeStructureState(FacilityStructureStateDocument? structure)
    {
        var mutable = NormalizeStructureStateMutable(structure);

        return new FacilityStructureState
        {
            AddedNodes = mutable.AddedNodes,
            RemovedNodeKeys = mutable.RemovedNodeKeys.ToHashSet(StringComparer.OrdinalIgnoreCase),
            ParentOverrides = new Dictionary<string, string?>(mutable.ParentOverrides, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static MutableStructureState NormalizeStructureStateMutable(FacilityStructureStateDocument? structure)
    {
        var normalized = new MutableStructureState();
        if (structure is null)
        {
            return normalized;
        }

        foreach (var addedNode in structure.AddedNodes)
        {
            var normalizedNode = NormalizeStructureNode(addedNode);
            normalized.ParentOverrides.Remove(normalizedNode.NodeKey);

            var existingIndex = normalized.AddedNodes.FindIndex(node =>
                node.NodeKey.Equals(normalizedNode.NodeKey, StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
            {
                normalized.AddedNodes[existingIndex] = normalizedNode;
            }
            else
            {
                normalized.AddedNodes.Add(normalizedNode);
            }
        }

        foreach (var removedNodeKey in structure.RemovedNodeKeys)
        {
            var normalizedNodeKey = NormalizeOptionalNodeKey(removedNodeKey);
            if (string.IsNullOrWhiteSpace(normalizedNodeKey))
            {
                continue;
            }

            normalized.RemovedNodeKeys.Add(normalizedNodeKey);
        }

        foreach (var pair in structure.ParentOverrides)
        {
            var normalizedNodeKey = NormalizeOptionalNodeKey(pair.Key);
            if (string.IsNullOrWhiteSpace(normalizedNodeKey))
            {
                continue;
            }

            var normalizedParentNodeKey = NormalizeOptionalNodeKey(pair.Value);
            if (!string.IsNullOrWhiteSpace(normalizedParentNodeKey)
                && normalizedNodeKey.Equals(normalizedParentNodeKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            normalized.ParentOverrides[normalizedNodeKey] = normalizedParentNodeKey;
        }

        normalized.AddedNodes.RemoveAll(node => normalized.RemovedNodeKeys.Contains(node.NodeKey));
        foreach (var removedNodeKey in normalized.RemovedNodeKeys)
        {
            normalized.ParentOverrides.Remove(removedNodeKey);
        }

        normalized.Sort();
        return normalized;
    }

    private static FacilityStructureNode NormalizeStructureNode(FacilityStructureNode node)
    {
        var nodeKey = NormalizeRequiredNodeKey(node.NodeKey);
        var label = NormalizeOptionalText(node.Label);
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new InvalidOperationException("Label nového uzlu musí být vyplněný.");
        }

        var parentNodeKey = NormalizeOptionalNodeKey(node.ParentNodeKey);
        if (!string.IsNullOrWhiteSpace(parentNodeKey)
            && nodeKey.Equals(parentNodeKey, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Node nelze napojit sám na sebe.");
        }

        return new FacilityStructureNode
        {
            NodeKey = nodeKey,
            Label = label,
            NodeType = NormalizeOptionalText(node.NodeType),
            Zone = NormalizeOptionalText(node.Zone),
            MeterUrn = NormalizeOptionalText(node.MeterUrn),
            ParentNodeKey = parentNodeKey,
            XHint = NormalizeHint(node.XHint),
            YHint = NormalizeHint(node.YHint),
            UpdatedUtc = node.UpdatedUtc == default ? DateTime.UtcNow : node.UpdatedUtc
        };
    }

    private static string NormalizeRequiredNodeKey(string? nodeKey)
    {
        var normalized = NormalizeOptionalNodeKey(nodeKey);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("NodeKey musí být vyplněný.");
        }

        return normalized;
    }

    private static string? NormalizeOptionalNodeKey(string? nodeKey)
    {
        if (string.IsNullOrWhiteSpace(nodeKey))
        {
            return null;
        }

        return nodeKey.Trim();
    }

    private static bool WouldCreateCycle(
        string movingNodeKey,
        string proposedParentNodeKey,
        IReadOnlyDictionary<string, string?> currentParentByNodeKey)
    {
        var childrenByParent = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (nodeKey, parentNodeKey) in currentParentByNodeKey)
        {
            if (string.IsNullOrWhiteSpace(parentNodeKey))
            {
                continue;
            }

            if (!childrenByParent.TryGetValue(parentNodeKey, out var children))
            {
                children = [];
                childrenByParent[parentNodeKey] = children;
            }

            children.Add(nodeKey);
        }

        var stack = new Stack<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        stack.Push(movingNodeKey);

        while (stack.Count > 0)
        {
            var currentNodeKey = stack.Pop();
            if (!visited.Add(currentNodeKey))
            {
                continue;
            }

            if (!childrenByParent.TryGetValue(currentNodeKey, out var children))
            {
                continue;
            }

            foreach (var childNodeKey in children)
            {
                if (childNodeKey.Equals(proposedParentNodeKey, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                stack.Push(childNodeKey);
            }
        }

        return false;
    }

    private static bool HasNodePayload(FacilityNodeEditorState nodeState)
    {
        return !string.IsNullOrWhiteSpace(nodeState.Label)
            || !string.IsNullOrWhiteSpace(nodeState.NodeType)
            || !string.IsNullOrWhiteSpace(nodeState.Zone)
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
            NodeType = NormalizeOptionalText(nodeState.NodeType),
            Zone = NormalizeOptionalText(nodeState.Zone),
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
        if (!hint.HasValue || !double.IsFinite(hint.Value))
            return null;
        return hint.Value;
    }

    private sealed class FacilityEditorStateDocument
    {
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public List<FacilityNodeEditorState> NodeEdits { get; set; } = [];
        public List<FacilitySavedWorkingSet> WorkingSets { get; set; } = [];
        public FacilityStructureStateDocument Structure { get; set; } = new();
    }

    private sealed class FacilityStructureStateDocument
    {
        public List<FacilityStructureNode> AddedNodes { get; set; } = [];
        public List<string> RemovedNodeKeys { get; set; } = [];
        public Dictionary<string, string?> ParentOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class MutableStructureState
    {
        public List<FacilityStructureNode> AddedNodes { get; } = [];
        public List<string> RemovedNodeKeys { get; } = [];
        public Dictionary<string, string?> ParentOverrides { get; } = new(StringComparer.OrdinalIgnoreCase);

        public FacilityStructureStateDocument ToDocument()
        {
            return new FacilityStructureStateDocument
            {
                AddedNodes = [.. AddedNodes],
                RemovedNodeKeys = [.. RemovedNodeKeys],
                ParentOverrides = new Dictionary<string, string?>(ParentOverrides, StringComparer.OrdinalIgnoreCase)
            };
        }

        public void Sort()
        {
            AddedNodes.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.NodeKey, right.NodeKey));
            RemovedNodeKeys.Sort(StringComparer.OrdinalIgnoreCase);
        }
    }
}
