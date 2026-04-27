using System.Text.Json;
using System.Text.Json.Serialization;
using DiplomovaPrace.Persistence.Schematic;

namespace DiplomovaPrace.Services;

public sealed class FacilityNodeEditorState
{
    public required string NodeKey { get; init; }
    public string? Label { get; init; }
    public string? NodeType { get; init; }
    public string? Zone { get; init; }
    public string? StylePresetKey { get; init; }
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

public enum FacilityImportedBindingFileFormat
{
    FixedCsvSeries
}

public sealed class FacilityImportedBindingState
{
    public required string BindingId { get; init; }
    public required string NodeKey { get; init; }
    public required string ExactSignalCode { get; init; }
    public required string Unit { get; init; }
    public required string OriginalFileName { get; init; }
    public required string StorageRelativePath { get; init; }
    public string? MeterUrn { get; init; }
    public string? SourceLabel { get; init; }
    public FacilityImportedBindingFileFormat FileFormat { get; init; } = FacilityImportedBindingFileFormat.FixedCsvSeries;
    public string Resolution { get; init; } = "irregular";
    public long? FileSizeBytes { get; init; }
    public DateTime ImportedUtc { get; init; } = DateTime.UtcNow;
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

public sealed class FacilityAdditionalRelationship
{
    public required string SourceNodeKey { get; init; }
    public required string TargetNodeKey { get; init; }
    public required string RelationshipKind { get; init; }
    public string? Note { get; init; }
    public DateTime UpdatedUtc { get; init; } = DateTime.UtcNow;
}

public sealed class FacilityStructureState
{
    public IReadOnlyList<FacilityStructureNode> AddedNodes { get; init; } = [];
    public IReadOnlySet<string> RemovedNodeKeys { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string?> ParentOverrides { get; init; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<FacilityAdditionalRelationship> AddedRelationships { get; init; } = [];
    public IReadOnlyList<FacilityAdditionalRelationship> RemovedRelationships { get; init; } = [];
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
    private const int CurrentSchemaVersion = 5;

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

    public async Task<IReadOnlyDictionary<string, FacilityNodeStylePreset>> GetStylePresetsByKeyAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);

        try
        {
            var state = await LoadStateUnsafeAsync(ct);
            return FacilityNodeStyleSystem.NormalizePresetLibrary(state.StylePresets);
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

    public async Task<IReadOnlyList<FacilityImportedBindingState>> GetImportedBindingsAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);

        try
        {
            var state = await LoadStateUnsafeAsync(ct);
            return state.ImportedBindings
                .GroupBy(binding => binding.BindingId, StringComparer.OrdinalIgnoreCase)
                .Select(group => NormalizeImportedBinding(group.Last()))
                .OrderBy(binding => binding.NodeKey, StringComparer.OrdinalIgnoreCase)
                .ThenBy(binding => binding.ExactSignalCode, StringComparer.OrdinalIgnoreCase)
                .ThenBy(binding => binding.ImportedUtc)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<FacilityImportedBindingState>> GetImportedBindingsByNodeKeyAsync(
        string? nodeKey,
        CancellationToken ct = default)
    {
        var normalizedNodeKey = NormalizeOptionalNodeKey(nodeKey);
        if (string.IsNullOrWhiteSpace(normalizedNodeKey))
        {
            return [];
        }

        await _gate.WaitAsync(ct);

        try
        {
            var state = await LoadStateUnsafeAsync(ct);
            return state.ImportedBindings
                .Where(binding => binding.NodeKey.Equals(normalizedNodeKey, StringComparison.OrdinalIgnoreCase))
                .GroupBy(binding => binding.BindingId, StringComparer.OrdinalIgnoreCase)
                .Select(group => NormalizeImportedBinding(group.Last()))
                .OrderBy(binding => binding.ExactSignalCode, StringComparer.OrdinalIgnoreCase)
                .ThenBy(binding => binding.ImportedUtc)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveImportedBindingAsync(FacilityImportedBindingState binding, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);

        try
        {
            var normalized = NormalizeImportedBinding(binding);
            var state = await LoadStateUnsafeAsync(ct);
            var existingIndex = state.ImportedBindings.FindIndex(existing =>
                existing.BindingId.Equals(normalized.BindingId, StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
            {
                state.ImportedBindings[existingIndex] = normalized;
            }
            else
            {
                state.ImportedBindings.Add(normalized);
            }

            state.ImportedBindings = state.ImportedBindings
                .GroupBy(existing => existing.BindingId, StringComparer.OrdinalIgnoreCase)
                .Select(group => NormalizeImportedBinding(group.Last()))
                .OrderBy(existing => existing.NodeKey, StringComparer.OrdinalIgnoreCase)
                .ThenBy(existing => existing.ExactSignalCode, StringComparer.OrdinalIgnoreCase)
                .ThenBy(existing => existing.ImportedUtc)
                .ToList();
            state.SchemaVersion = CurrentSchemaVersion;

            await PersistStateUnsafeAsync(state, ct);
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

    public async Task<FacilityStructureEditResult> TryAddAdditionalRelationshipAsync(
        FacilityAdditionalRelationship relationship,
        IReadOnlySet<string> currentNodeKeys,
        IEnumerable<FacilityAdditionalRelationship> currentRelationships,
        CancellationToken ct = default)
    {
        FacilityAdditionalRelationship normalizedRelationship;
        try
        {
            normalizedRelationship = NormalizeAdditionalRelationship(relationship);
        }
        catch (InvalidOperationException ex)
        {
            return FacilityStructureEditResult.Fail(ex.Message);
        }

        if (!currentNodeKeys.Contains(normalizedRelationship.SourceNodeKey))
        {
            return FacilityStructureEditResult.Fail($"Zdrojový uzel {normalizedRelationship.SourceNodeKey} neexistuje.");
        }

        if (!currentNodeKeys.Contains(normalizedRelationship.TargetNodeKey))
        {
            return FacilityStructureEditResult.Fail($"Cílový uzel {normalizedRelationship.TargetNodeKey} neexistuje.");
        }

        var relationshipKey = BuildAdditionalRelationshipKey(normalizedRelationship);
        var existingRelationshipKeys = currentRelationships
            .Select(BuildAdditionalRelationshipKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (existingRelationshipKeys.Contains(relationshipKey))
        {
            return FacilityStructureEditResult.Fail("Stejná explicitní vazba už existuje.");
        }

        await _gate.WaitAsync(ct);

        try
        {
            var state = await LoadStateUnsafeAsync(ct);
            var normalizedStructure = NormalizeStructureStateMutable(state.Structure);

            normalizedStructure.RemoveRemovedRelationship(relationshipKey);
            normalizedStructure.UpsertAddedRelationship(normalizedRelationship);

            normalizedStructure.Sort();
            state.Structure = normalizedStructure.ToDocument();
            state.SchemaVersion = CurrentSchemaVersion;
            await PersistStateUnsafeAsync(state, ct);

            return FacilityStructureEditResult.Ok($"Explicitní vazba {normalizedRelationship.SourceNodeKey} → {normalizedRelationship.TargetNodeKey} ({normalizedRelationship.RelationshipKind}) byla přidána.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<FacilityStructureEditResult> TryRemoveAdditionalRelationshipAsync(
        FacilityAdditionalRelationship relationship,
        IEnumerable<FacilityAdditionalRelationship> currentRelationships,
        CancellationToken ct = default)
    {
        FacilityAdditionalRelationship normalizedRelationship;
        try
        {
            normalizedRelationship = NormalizeAdditionalRelationship(relationship);
        }
        catch (InvalidOperationException ex)
        {
            return FacilityStructureEditResult.Fail(ex.Message);
        }

        var relationshipKey = BuildAdditionalRelationshipKey(normalizedRelationship);
        var existingRelationshipKeys = currentRelationships
            .Select(BuildAdditionalRelationshipKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!existingRelationshipKeys.Contains(relationshipKey))
        {
            return FacilityStructureEditResult.Fail("Vybraná explicitní vazba už v aktuálním draftu neexistuje.");
        }

        await _gate.WaitAsync(ct);

        try
        {
            var state = await LoadStateUnsafeAsync(ct);
            var normalizedStructure = NormalizeStructureStateMutable(state.Structure);

            var removedFromAdded = normalizedStructure.RemoveAddedRelationship(relationshipKey);
            if (removedFromAdded == 0)
            {
                normalizedStructure.UpsertRemovedRelationship(normalizedRelationship);
            }

            normalizedStructure.Sort();
            state.Structure = normalizedStructure.ToDocument();
            state.SchemaVersion = CurrentSchemaVersion;
            await PersistStateUnsafeAsync(state, ct);

            return FacilityStructureEditResult.Ok($"Explicitní vazba {normalizedRelationship.SourceNodeKey} → {normalizedRelationship.TargetNodeKey} ({normalizedRelationship.RelationshipKind}) byla odstraněna.");
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

    public async Task SaveStylePresetsAsync(IEnumerable<FacilityNodeStylePreset> stylePresets, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);

        try
        {
            var state = await LoadStateUnsafeAsync(ct);
            var normalizedLibrary = FacilityNodeStyleSystem.NormalizePresetLibrary(stylePresets);

            state.SchemaVersion = CurrentSchemaVersion;
            state.StylePresets = normalizedLibrary.Values
                .OrderBy(preset => preset.Key.Equals(FacilityNodeStyleSystem.DefaultPresetKey, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(preset => preset.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(preset => preset.Key, StringComparer.OrdinalIgnoreCase)
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
            ParentOverrides = new Dictionary<string, string?>(mutable.ParentOverrides, StringComparer.OrdinalIgnoreCase),
            AddedRelationships = [.. mutable.AddedRelationships],
            RemovedRelationships = [.. mutable.RemovedRelationships]
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

        foreach (var relationship in structure.AddedRelationships)
        {
            var normalizedRelationship = NormalizeAdditionalRelationship(relationship);
            normalized.RemoveRemovedRelationship(BuildAdditionalRelationshipKey(normalizedRelationship));
            normalized.UpsertAddedRelationship(normalizedRelationship);
        }

        foreach (var relationship in structure.RemovedRelationships)
        {
            var normalizedRelationship = NormalizeAdditionalRelationship(relationship);
            normalized.UpsertRemovedRelationship(normalizedRelationship);
        }

        normalized.AddedNodes.RemoveAll(node => normalized.RemovedNodeKeys.Contains(node.NodeKey));
        foreach (var removedNodeKey in normalized.RemovedNodeKeys)
        {
            normalized.ParentOverrides.Remove(removedNodeKey);
        }

        normalized.AddedRelationships.RemoveAll(relationship =>
            normalized.RemovedNodeKeys.Contains(relationship.SourceNodeKey)
            || normalized.RemovedNodeKeys.Contains(relationship.TargetNodeKey));
        normalized.RemovedRelationships.RemoveAll(relationship =>
            normalized.RemovedNodeKeys.Contains(relationship.SourceNodeKey)
            || normalized.RemovedNodeKeys.Contains(relationship.TargetNodeKey));

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

    private static FacilityAdditionalRelationship NormalizeAdditionalRelationship(FacilityAdditionalRelationship relationship)
    {
        var sourceNodeKey = NormalizeRequiredNodeKey(relationship.SourceNodeKey);
        var targetNodeKey = NormalizeRequiredNodeKey(relationship.TargetNodeKey);
        if (sourceNodeKey.Equals(targetNodeKey, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Explicitní vazba nemůže spojovat uzel sám na sebe.");
        }

        var relationshipKind = NormalizeRelationshipKind(relationship.RelationshipKind);

        return new FacilityAdditionalRelationship
        {
            SourceNodeKey = sourceNodeKey,
            TargetNodeKey = targetNodeKey,
            RelationshipKind = relationshipKind,
            Note = NormalizeOptionalText(relationship.Note),
            UpdatedUtc = relationship.UpdatedUtc == default ? DateTime.UtcNow : relationship.UpdatedUtc
        };
    }

    private static string NormalizeRelationshipKind(string? relationshipKind)
    {
        var normalized = NormalizeOptionalText(relationshipKind);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Relationship kind musí být vyplněný.");
        }

        normalized = normalized.ToLowerInvariant();
        if (string.Equals(normalized, SchematicRelationshipKinds.LayoutPrimary, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("layout_primary se upravuje pouze přes primary layout parent.");
        }

        return normalized;
    }

    private static string BuildAdditionalRelationshipKey(FacilityAdditionalRelationship relationship)
    {
        return BuildAdditionalRelationshipKey(relationship.SourceNodeKey, relationship.TargetNodeKey, relationship.RelationshipKind);
    }

    private static string BuildAdditionalRelationshipKey(string sourceNodeKey, string targetNodeKey, string relationshipKind)
    {
        return string.Create(
            sourceNodeKey.Length + targetNodeKey.Length + relationshipKind.Length + 2,
            (sourceNodeKey, targetNodeKey, relationshipKind),
            static (span, state) =>
            {
                var offset = 0;
                state.sourceNodeKey.AsSpan().CopyTo(span[offset..]);
                offset += state.sourceNodeKey.Length;
                span[offset++] = '|';
                state.targetNodeKey.AsSpan().CopyTo(span[offset..]);
                offset += state.targetNodeKey.Length;
                span[offset++] = '|';
                state.relationshipKind.AsSpan().CopyTo(span[offset..]);
            });
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
            || !string.IsNullOrWhiteSpace(nodeState.StylePresetKey)
            || nodeState.XHint.HasValue
            || nodeState.YHint.HasValue
            || !string.IsNullOrWhiteSpace(nodeState.Note)
            || nodeState.Tags.Count > 0;
    }

    private static FacilityImportedBindingState NormalizeImportedBinding(FacilityImportedBindingState binding)
    {
        var bindingId = binding.BindingId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(bindingId))
        {
            throw new InvalidOperationException("BindingId musí být vyplněný.");
        }

        var nodeKey = binding.NodeKey?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nodeKey))
        {
            throw new InvalidOperationException("NodeKey bindingu musí být vyplněný.");
        }

        if (!FacilitySignalTaxonomy.TryParseExactCode(binding.ExactSignalCode, out var exactSignalCode) || exactSignalCode.IsEmpty)
        {
            throw new InvalidOperationException("Exact signal code bindingu musí být vyplněný.");
        }

        var unit = binding.Unit?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new InvalidOperationException("Unit bindingu musí být vyplněná.");
        }

        var originalFileName = Path.GetFileName(binding.OriginalFileName?.Trim() ?? string.Empty);
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new InvalidOperationException("Original file name bindingu musí být vyplněný.");
        }

        var storageRelativePath = NormalizeStorageRelativePath(binding.StorageRelativePath);

        return new FacilityImportedBindingState
        {
            BindingId = bindingId,
            NodeKey = nodeKey,
            ExactSignalCode = exactSignalCode.Value,
            Unit = unit,
            OriginalFileName = originalFileName,
            StorageRelativePath = storageRelativePath,
            MeterUrn = NormalizeOptionalText(binding.MeterUrn),
            SourceLabel = NormalizeOptionalText(binding.SourceLabel),
            FileFormat = binding.FileFormat,
            Resolution = string.IsNullOrWhiteSpace(binding.Resolution) ? "irregular" : binding.Resolution.Trim(),
            FileSizeBytes = binding.FileSizeBytes.HasValue && binding.FileSizeBytes.Value > 0 ? binding.FileSizeBytes.Value : null,
            ImportedUtc = binding.ImportedUtc == default ? DateTime.UtcNow : binding.ImportedUtc
        };
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
            StylePresetKey = NormalizeOptionalStylePresetKey(nodeState.StylePresetKey),
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

        var normalized = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawTag in tags)
        {
            if (string.IsNullOrWhiteSpace(rawTag))
            {
                continue;
            }

            var trimmed = rawTag.Trim();
            if (seen.Add(trimmed))
            {
                normalized.Add(trimmed);
            }
        }

        return normalized
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

    private static string? NormalizeOptionalStylePresetKey(string? raw)
    {
        var normalized = FacilityNodeStyleSystem.NormalizePresetKey(raw);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string NormalizeStorageRelativePath(string? raw)
    {
        var normalized = raw?.Trim().Replace('\\', '/') ?? string.Empty;
        normalized = normalized.TrimStart('/');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("StorageRelativePath bindingu musí být vyplněná.");
        }

        if (Path.IsPathRooted(normalized))
        {
            throw new InvalidOperationException("StorageRelativePath bindingu nesmí být absolutní cesta.");
        }

        if (normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).Any(segment => segment == ".."))
        {
            throw new InvalidOperationException("StorageRelativePath bindingu nesmí obsahovat '..'.");
        }

        return normalized;
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
        public List<FacilityImportedBindingState> ImportedBindings { get; set; } = [];
        public List<FacilityNodeStylePreset> StylePresets { get; set; } = [];
        public List<FacilitySavedWorkingSet> WorkingSets { get; set; } = [];
        public FacilityStructureStateDocument Structure { get; set; } = new();
    }

    private sealed class FacilityStructureStateDocument
    {
        public List<FacilityStructureNode> AddedNodes { get; set; } = [];
        public List<string> RemovedNodeKeys { get; set; } = [];
        public Dictionary<string, string?> ParentOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<FacilityAdditionalRelationship> AddedRelationships { get; set; } = [];
        public List<FacilityAdditionalRelationship> RemovedRelationships { get; set; } = [];
    }

    private sealed class MutableStructureState
    {
        public List<FacilityStructureNode> AddedNodes { get; } = [];
        public List<string> RemovedNodeKeys { get; } = [];
        public Dictionary<string, string?> ParentOverrides { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<FacilityAdditionalRelationship> AddedRelationships { get; } = [];
        public List<FacilityAdditionalRelationship> RemovedRelationships { get; } = [];

        public FacilityStructureStateDocument ToDocument()
        {
            return new FacilityStructureStateDocument
            {
                AddedNodes = [.. AddedNodes],
                RemovedNodeKeys = [.. RemovedNodeKeys],
                ParentOverrides = new Dictionary<string, string?>(ParentOverrides, StringComparer.OrdinalIgnoreCase),
                AddedRelationships = [.. AddedRelationships],
                RemovedRelationships = [.. RemovedRelationships]
            };
        }

        public void UpsertAddedRelationship(FacilityAdditionalRelationship relationship)
        {
            var relationshipKey = BuildAdditionalRelationshipKey(relationship);
            RemoveRemovedRelationship(relationshipKey);

            var existingIndex = AddedRelationships.FindIndex(existing =>
                BuildAdditionalRelationshipKey(existing).Equals(relationshipKey, StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
            {
                AddedRelationships[existingIndex] = relationship;
            }
            else
            {
                AddedRelationships.Add(relationship);
            }
        }

        public void UpsertRemovedRelationship(FacilityAdditionalRelationship relationship)
        {
            var relationshipKey = BuildAdditionalRelationshipKey(relationship);
            var existingIndex = RemovedRelationships.FindIndex(existing =>
                BuildAdditionalRelationshipKey(existing).Equals(relationshipKey, StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
            {
                RemovedRelationships[existingIndex] = relationship;
            }
            else
            {
                RemovedRelationships.Add(relationship);
            }
        }

        public int RemoveAddedRelationship(string relationshipKey)
        {
            return AddedRelationships.RemoveAll(existing =>
                BuildAdditionalRelationshipKey(existing).Equals(relationshipKey, StringComparison.OrdinalIgnoreCase));
        }

        public int RemoveRemovedRelationship(string relationshipKey)
        {
            return RemovedRelationships.RemoveAll(existing =>
                BuildAdditionalRelationshipKey(existing).Equals(relationshipKey, StringComparison.OrdinalIgnoreCase));
        }

        public void Sort()
        {
            AddedNodes.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.NodeKey, right.NodeKey));
            RemovedNodeKeys.Sort(StringComparer.OrdinalIgnoreCase);
            AddedRelationships.Sort(static (left, right) => StringComparer.OrdinalIgnoreCase.Compare(
                BuildAdditionalRelationshipKey(left),
                BuildAdditionalRelationshipKey(right)));
            RemovedRelationships.Sort(static (left, right) => StringComparer.OrdinalIgnoreCase.Compare(
                BuildAdditionalRelationshipKey(left),
                BuildAdditionalRelationshipKey(right)));
        }
    }
}
