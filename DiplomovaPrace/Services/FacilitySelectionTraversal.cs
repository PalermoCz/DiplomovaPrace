using DiplomovaPrace.Persistence.Schematic;

namespace DiplomovaPrace.Services;

public enum FacilitySelectionTraversalMode
{
    StrictLayoutSubtree,
    ExpandedSubtree
}

public sealed class FacilitySelectionTraversalResult
{
    public required IReadOnlySet<string> IncludedNodeKeys { get; init; }
    public required IReadOnlyList<string> ConcreteNodeKeys { get; init; }
}

public static class FacilitySelectionTraversal
{
    public static FacilitySelectionTraversalResult Resolve(
        string rootNodeKey,
        IReadOnlyDictionary<string, SchematicNodeEntity> nodesByKey,
        IReadOnlyDictionary<string, List<SchematicNodeEntity>> childrenByParentKey,
        IEnumerable<SchematicEdgeEntity> topologyEdges,
        FacilitySelectionTraversalMode mode,
        IReadOnlySet<string> allowedRelationshipKinds,
        Func<SchematicNodeEntity, bool> isConcreteSelectable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootNodeKey);
        ArgumentNullException.ThrowIfNull(nodesByKey);
        ArgumentNullException.ThrowIfNull(childrenByParentKey);
        ArgumentNullException.ThrowIfNull(topologyEdges);
        ArgumentNullException.ThrowIfNull(allowedRelationshipKinds);
        ArgumentNullException.ThrowIfNull(isConcreteSelectable);

        if (!nodesByKey.ContainsKey(rootNodeKey))
        {
            return Empty();
        }

        var includedNodeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ExpandLayoutClosure(rootNodeKey, childrenByParentKey, includedNodeKeys, null);

        if (mode == FacilitySelectionTraversalMode.ExpandedSubtree)
        {
            var allowedAdjacency = BuildAllowedAdjacency(topologyEdges, allowedRelationshipKinds);
            var expansionQueue = new Queue<string>(includedNodeKeys);

            while (expansionQueue.Count > 0)
            {
                var currentNodeKey = expansionQueue.Dequeue();
                if (!allowedAdjacency.TryGetValue(currentNodeKey, out var relatedNodeKeys))
                {
                    continue;
                }

                foreach (var relatedNodeKey in relatedNodeKeys)
                {
                    if (!nodesByKey.ContainsKey(relatedNodeKey) || includedNodeKeys.Contains(relatedNodeKey))
                    {
                        continue;
                    }

                    ExpandLayoutClosure(relatedNodeKey, childrenByParentKey, includedNodeKeys, expansionQueue);
                }
            }
        }

        var concreteNodeKeys = includedNodeKeys
            .Where(nodesByKey.ContainsKey)
            .Select(nodeKey => nodesByKey[nodeKey])
            .Where(isConcreteSelectable)
            .OrderBy(node => node.Label ?? node.NodeKey, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(node => node.NodeKey, StringComparer.OrdinalIgnoreCase)
            .Select(node => node.NodeKey)
            .ToList();

        return new FacilitySelectionTraversalResult
        {
            IncludedNodeKeys = includedNodeKeys,
            ConcreteNodeKeys = concreteNodeKeys
        };
    }

    private static Dictionary<string, List<string>> BuildAllowedAdjacency(
        IEnumerable<SchematicEdgeEntity> topologyEdges,
        IReadOnlySet<string> allowedRelationshipKinds)
    {
        var adjacency = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var edge in topologyEdges)
        {
            if (edge is null
                || string.IsNullOrWhiteSpace(edge.SourceNodeKey)
                || string.IsNullOrWhiteSpace(edge.TargetNodeKey)
                || string.IsNullOrWhiteSpace(edge.RelationshipKind))
            {
                continue;
            }

            if (edge.IsLayoutEdge
                || edge.RelationshipKind.Equals(SchematicRelationshipKinds.LayoutPrimary, StringComparison.OrdinalIgnoreCase)
                || !allowedRelationshipKinds.Contains(edge.RelationshipKind))
            {
                continue;
            }

            if (!adjacency.TryGetValue(edge.SourceNodeKey, out var targets))
            {
                targets = [];
                adjacency[edge.SourceNodeKey] = targets;
            }

            if (!targets.Any(target => target.Equals(edge.TargetNodeKey, StringComparison.OrdinalIgnoreCase)))
            {
                targets.Add(edge.TargetNodeKey);
            }
        }

        return adjacency;
    }

    private static void ExpandLayoutClosure(
        string rootNodeKey,
        IReadOnlyDictionary<string, List<SchematicNodeEntity>> childrenByParentKey,
        ISet<string> includedNodeKeys,
        Queue<string>? expansionQueue)
    {
        var stack = new Stack<string>();
        stack.Push(rootNodeKey);

        while (stack.Count > 0)
        {
            var currentNodeKey = stack.Pop();
            if (!includedNodeKeys.Add(currentNodeKey))
            {
                continue;
            }

            expansionQueue?.Enqueue(currentNodeKey);

            if (!childrenByParentKey.TryGetValue(currentNodeKey, out var children) || children.Count == 0)
            {
                continue;
            }

            foreach (var child in children)
            {
                stack.Push(child.NodeKey);
            }
        }
    }

    private static FacilitySelectionTraversalResult Empty()
    {
        return new FacilitySelectionTraversalResult
        {
            IncludedNodeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            ConcreteNodeKeys = []
        };
    }
}