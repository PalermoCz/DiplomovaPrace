using DiplomovaPrace.Persistence;
using DiplomovaPrace.Persistence.Schematic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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

    public async Task<FacilityEntity?> GetFacilityAsync(int facilityId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var facility = await db.Facilities
            .AsNoTracking()
            .Include(f => f.Nodes)
            .Include(f => f.Edges)
            .FirstOrDefaultAsync(f => f.Id == facilityId, ct);

        if (facility is null)
        {
            return null;
        }

        NormalizeEdgeMetadata(facility);

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
            || structureState.ParentOverrides.Count > 0
            || structureState.AddedRelationships.Count > 0
            || structureState.RemovedRelationships.Count > 0;
    }

    private static void ApplyStructureEdits(FacilityEntity facility, FacilityStructureState structureState)
    {
        var removedNodeKeys = structureState.RemovedNodeKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var preservedEdges = facility.Edges
            .Where(edge => !removedNodeKeys.Contains(edge.SourceNodeKey) && !removedNodeKeys.Contains(edge.TargetNodeKey))
            .Select(CloneEdge)
            .ToList();
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

        var effectiveExplicitEdges = ApplyAdditionalRelationshipEdits(
            facility.Id,
            preservedEdges,
            effectiveNodes,
            structureState);

        facility.Edges = BuildEffectiveEdges(facility.Id, effectiveNodes, effectiveExplicitEdges);
    }

    private static List<SchematicEdgeEntity> ApplyAdditionalRelationshipEdits(
        int facilityId,
        IReadOnlyList<SchematicEdgeEntity> preservedEdges,
        IReadOnlyDictionary<string, SchematicNodeEntity> effectiveNodes,
        FacilityStructureState structureState)
    {
        var nodeKeys = effectiveNodes.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var removedRelationshipKeys = structureState.RemovedRelationships
            .Select(BuildEdgeKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var result = preservedEdges
            .Where(edge => nodeKeys.Contains(edge.SourceNodeKey) && nodeKeys.Contains(edge.TargetNodeKey))
            .Where(edge => !IsLayoutRelationship(edge))
            .Where(edge => !removedRelationshipKeys.Contains(BuildEdgeKey(edge)))
            .Select(edge =>
            {
                var clone = CloneEdge(edge);
                clone.FacilityId = facilityId;
                clone.RelationshipKind = NormalizeRelationshipKind(clone.RelationshipKind, clone.IsLayoutEdge);
                clone.IsLayoutEdge = false;
                return clone;
            })
            .ToList();

        foreach (var relationship in structureState.AddedRelationships)
        {
            if (!nodeKeys.Contains(relationship.SourceNodeKey) || !nodeKeys.Contains(relationship.TargetNodeKey))
            {
                continue;
            }

            result.Add(new SchematicEdgeEntity
            {
                FacilityId = facilityId,
                SourceNodeKey = relationship.SourceNodeKey,
                TargetNodeKey = relationship.TargetNodeKey,
                RelationshipKind = NormalizeRelationshipKind(relationship.RelationshipKind, isLayoutEdge: false),
                IsLayoutEdge = false,
                Note = relationship.Note
            });
        }

        return result;
    }

    private static void NormalizeEdgeMetadata(FacilityEntity facility)
    {
        var nodeByKey = facility.Nodes
            .ToDictionary(node => node.NodeKey, StringComparer.OrdinalIgnoreCase);

        foreach (var edge in facility.Edges)
        {
            var isLayoutProjection = nodeByKey.TryGetValue(edge.TargetNodeKey, out var targetNode)
                && !string.IsNullOrWhiteSpace(targetNode.ParentNodeKey)
                && targetNode.ParentNodeKey.Equals(edge.SourceNodeKey, StringComparison.OrdinalIgnoreCase);

            if (isLayoutProjection)
            {
                edge.RelationshipKind = SchematicRelationshipKinds.LayoutPrimary;
                edge.IsLayoutEdge = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(edge.RelationshipKind))
            {
                edge.RelationshipKind = edge.IsLayoutEdge
                    ? SchematicRelationshipKinds.LayoutPrimary
                    : SchematicRelationshipKinds.Semantic;
            }

            edge.RelationshipKind = edge.RelationshipKind.Trim().ToLowerInvariant();
            edge.IsLayoutEdge = edge.IsLayoutEdge
                || edge.RelationshipKind.Equals(SchematicRelationshipKinds.LayoutPrimary, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static List<SchematicEdgeEntity> BuildEffectiveEdges(
        int facilityId,
        IReadOnlyDictionary<string, SchematicNodeEntity> effectiveNodes,
        IEnumerable<SchematicEdgeEntity> preservedEdges)
    {
        var nodeKeys = effectiveNodes.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var topologyEdges = preservedEdges
            .Where(edge => nodeKeys.Contains(edge.SourceNodeKey) && nodeKeys.Contains(edge.TargetNodeKey))
            .Where(edge => !IsLayoutRelationship(edge))
            .Select(edge =>
            {
                var clone = CloneEdge(edge);
                clone.RelationshipKind = NormalizeRelationshipKind(clone.RelationshipKind, clone.IsLayoutEdge);
                clone.IsLayoutEdge = false;
                return clone;
            })
            .ToList();

        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<SchematicEdgeEntity>();

        foreach (var edge in topologyEdges)
        {
            var key = BuildEdgeKey(edge.SourceNodeKey, edge.TargetNodeKey, edge.RelationshipKind);
            if (!seenKeys.Add(key))
            {
                continue;
            }

            edge.FacilityId = facilityId;
            result.Add(edge);
        }

        foreach (var node in effectiveNodes.Values)
        {
            if (string.IsNullOrWhiteSpace(node.ParentNodeKey))
            {
                continue;
            }

            var layoutEdge = new SchematicEdgeEntity
            {
                FacilityId = facilityId,
                SourceNodeKey = node.ParentNodeKey,
                TargetNodeKey = node.NodeKey,
                RelationshipKind = SchematicRelationshipKinds.LayoutPrimary,
                IsLayoutEdge = true
            };

            var key = BuildEdgeKey(layoutEdge.SourceNodeKey, layoutEdge.TargetNodeKey, layoutEdge.RelationshipKind);
            if (!seenKeys.Add(key))
            {
                continue;
            }

            result.Add(layoutEdge);
        }

        return result
            .OrderBy(edge => edge.SourceNodeKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(edge => edge.TargetNodeKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(edge => edge.RelationshipKind, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsLayoutRelationship(SchematicEdgeEntity edge)
    {
        return edge.IsLayoutEdge
            || edge.RelationshipKind.Equals(SchematicRelationshipKinds.LayoutPrimary, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeRelationshipKind(string? relationshipKind, bool isLayoutEdge)
    {
        var normalized = relationshipKind?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        return isLayoutEdge
            ? SchematicRelationshipKinds.LayoutPrimary
            : SchematicRelationshipKinds.Semantic;
    }

    private static string BuildEdgeKey(string sourceNodeKey, string targetNodeKey, string relationshipKind)
    {
        return $"{sourceNodeKey}|{targetNodeKey}|{relationshipKind}";
    }

    private static string BuildEdgeKey(FacilityAdditionalRelationship relationship)
    {
        return BuildEdgeKey(relationship.SourceNodeKey, relationship.TargetNodeKey, relationship.RelationshipKind);
    }

    private static string BuildEdgeKey(SchematicEdgeEntity edge)
    {
        return BuildEdgeKey(edge.SourceNodeKey, edge.TargetNodeKey, NormalizeRelationshipKind(edge.RelationshipKind, edge.IsLayoutEdge));
    }

    private static SchematicEdgeEntity CloneEdge(SchematicEdgeEntity source)
    {
        return new SchematicEdgeEntity
        {
            Id = source.Id,
            FacilityId = source.FacilityId,
            SourceNodeKey = source.SourceNodeKey,
            TargetNodeKey = source.TargetNodeKey,
            RelationshipKind = source.RelationshipKind,
            IsLayoutEdge = source.IsLayoutEdge,
            Note = source.Note
        };
    }
}
