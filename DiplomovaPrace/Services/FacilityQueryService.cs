using DiplomovaPrace.Persistence;
using DiplomovaPrace.Persistence.Schematic;
using Microsoft.EntityFrameworkCore;

namespace DiplomovaPrace.Services;

public class FacilityQueryService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly FacilityEditorStateService _editorStateService;

    public FacilityQueryService(
        IDbContextFactory<AppDbContext> dbFactory,
        FacilityEditorStateService editorStateService)
    {
        _dbFactory = dbFactory;
        _editorStateService = editorStateService;
    }

    public async Task<FacilityEntity?> GetMainFacilityAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var facility = await db.Facilities
            .AsNoTracking()
            .Include(f => f.Nodes)
            .Include(f => f.Edges)
            .FirstOrDefaultAsync(f => f.Name == "Smart Company Facility", ct);

        if (facility is null)
        {
            return null;
        }

        var structureStateTask = _editorStateService.GetStructureStateAsync(ct);
        var nodeStatesByKeyTask = _editorStateService.GetNodeStatesByKeyAsync(ct);

        await Task.WhenAll(structureStateTask, nodeStatesByKeyTask);

        var structureState = structureStateTask.Result;
        var nodeStatesByKey = nodeStatesByKeyTask.Result;

        if (HasStructuralEdits(structureState))
        {
            ApplyStructureEdits(facility, structureState);
        }

        foreach (var node in facility.Nodes)
        {
            if (!nodeStatesByKey.TryGetValue(node.NodeKey, out var nodeState))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(nodeState.Label))
            {
                node.Label = nodeState.Label;
            }

            if (!string.IsNullOrWhiteSpace(nodeState.NodeType))
            {
                node.NodeType = nodeState.NodeType;
            }

            if (!string.IsNullOrWhiteSpace(nodeState.Zone))
            {
                node.Zone = nodeState.Zone;
            }

            if (nodeState.XHint.HasValue)
            {
                node.XHint = nodeState.XHint.Value;
            }

            if (nodeState.YHint.HasValue)
            {
                node.YHint = nodeState.YHint.Value;
            }
        }

        return facility;
    }

    private static bool HasStructuralEdits(FacilityStructureState structureState)
    {
        return structureState.AddedNodes.Count > 0
            || structureState.RemovedNodeKeys.Count > 0
            || structureState.ParentOverrides.Count > 0;
    }

    private static void ApplyStructureEdits(FacilityEntity facility, FacilityStructureState structureState)
    {
        var removedNodeKeys = structureState.RemovedNodeKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var effectiveNodes = facility.Nodes
            .Where(node => !removedNodeKeys.Contains(node.NodeKey))
            .ToDictionary(node => node.NodeKey, StringComparer.OrdinalIgnoreCase);

        foreach (var addedNode in structureState.AddedNodes)
        {
            if (removedNodeKeys.Contains(addedNode.NodeKey))
            {
                continue;
            }

            if (effectiveNodes.ContainsKey(addedNode.NodeKey))
            {
                continue;
            }

            effectiveNodes[addedNode.NodeKey] = new SchematicNodeEntity
            {
                FacilityId = facility.Id,
                NodeKey = addedNode.NodeKey,
                Label = addedNode.Label,
                NodeType = addedNode.NodeType,
                Zone = addedNode.Zone,
                MeterUrn = addedNode.MeterUrn,
                ParentNodeKey = addedNode.ParentNodeKey,
                XHint = addedNode.XHint,
                YHint = addedNode.YHint
            };
        }

        foreach (var (nodeKey, parentNodeKeyOverride) in structureState.ParentOverrides)
        {
            if (!effectiveNodes.TryGetValue(nodeKey, out var node))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(parentNodeKeyOverride))
            {
                node.ParentNodeKey = null;
                continue;
            }

            if (!effectiveNodes.ContainsKey(parentNodeKeyOverride)
                || parentNodeKeyOverride.Equals(node.NodeKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            node.ParentNodeKey = parentNodeKeyOverride;
        }

        foreach (var node in effectiveNodes.Values)
        {
            if (string.IsNullOrWhiteSpace(node.ParentNodeKey))
            {
                continue;
            }

            if (!effectiveNodes.ContainsKey(node.ParentNodeKey)
                || node.ParentNodeKey.Equals(node.NodeKey, StringComparison.OrdinalIgnoreCase))
            {
                node.ParentNodeKey = null;
            }
        }

        foreach (var node in effectiveNodes.Values)
        {
            if (string.IsNullOrWhiteSpace(node.ParentNodeKey))
            {
                continue;
            }

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                node.NodeKey
            };
            var cursor = node.ParentNodeKey;

            while (!string.IsNullOrWhiteSpace(cursor) && effectiveNodes.TryGetValue(cursor, out var parentNode))
            {
                if (!visited.Add(parentNode.NodeKey))
                {
                    node.ParentNodeKey = null;
                    break;
                }

                cursor = parentNode.ParentNodeKey;
            }
        }

        facility.Nodes = effectiveNodes.Values
            .OrderBy(node => node.NodeKey, StringComparer.OrdinalIgnoreCase)
            .ToList();

        facility.Edges = facility.Nodes
            .Where(node => !string.IsNullOrWhiteSpace(node.ParentNodeKey))
            .Select(node => new SchematicEdgeEntity
            {
                FacilityId = facility.Id,
                SourceNodeKey = node.ParentNodeKey!,
                TargetNodeKey = node.NodeKey
            })
            .OrderBy(edge => edge.SourceNodeKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(edge => edge.TargetNodeKey, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
