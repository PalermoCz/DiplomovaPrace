using DiplomovaPrace.Persistence.Schematic;

namespace DiplomovaPrace.Services;

/// <summary>
/// Deterministicky pripravuje schematic layout facility grafu.
/// Zaklad je hierarchy-aware left-to-right rozmisteni s minimalnim vertikalnim spacingem.
/// </summary>
public static class FacilitySchematicLayoutService
{
    private const double MinX = 0.08;
    private const double MaxX = 0.92;
    private const double MinY = 0.08;
    private const double MaxY = 0.92;
    private const double RootSeparationUnits = 1.4;
    private const double PreferredDepthGap = 0.065;
    private const double AbsoluteMinDepthGap = 0.03;

    public static FacilityEntity BuildReadableLayout(FacilityEntity source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var sourceNodes = source.Nodes.ToList();
        var sourceEdges = source.Edges.ToList();

        if (sourceNodes.Count == 0)
        {
            return CloneFacility(source, [], sourceEdges.Select(CloneEdge).ToList());
        }

        var nodeByKey = sourceNodes
            .GroupBy(node => node.NodeKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToDictionary(node => node.NodeKey, StringComparer.OrdinalIgnoreCase);

        var nodeKeys = nodeByKey.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var fallbackParentByNodeKey = BuildFallbackParentLookup(sourceEdges, nodeKeys);
        var parentByNodeKey = BuildParentLookup(nodeByKey.Values, nodeKeys, fallbackParentByNodeKey);
        var childrenByParentKey = BuildChildrenLookup(nodeByKey.Values, parentByNodeKey);
        var roots = BuildRootList(nodeByKey.Values, parentByNodeKey);
        var depthByNodeKey = BuildDepthLookup(nodeByKey.Values, roots, childrenByParentKey);
        var yByNodeKey = BuildYLookup(nodeByKey.Values, roots, childrenByParentKey);
        var adjustedYByNodeKey = ApplyDepthSpacing(nodeByKey.Values, depthByNodeKey, yByNodeKey);

        var maxDepth = depthByNodeKey.Values.DefaultIfEmpty(0).Max();
        var positionedNodes = nodeByKey.Values
            .Select(node =>
            {
                var clone = CloneNode(node);

                if (depthByNodeKey.TryGetValue(node.NodeKey, out var depth))
                {
                    clone.XHint = ComputeX(depth, maxDepth);
                }

                if (adjustedYByNodeKey.TryGetValue(node.NodeKey, out var y))
                {
                    clone.YHint = y;
                }

                return clone;
            })
            .ToList();

        var edges = sourceEdges
            .Where(edge => nodeKeys.Contains(edge.SourceNodeKey) && nodeKeys.Contains(edge.TargetNodeKey))
            .Select(CloneEdge)
            .ToList();

        return CloneFacility(source, positionedNodes, edges);
    }

    private static Dictionary<string, string?> BuildParentLookup(
        IEnumerable<SchematicNodeEntity> nodes,
        IReadOnlySet<string> nodeKeys,
        IReadOnlyDictionary<string, string> fallbackParentByNodeKey)
    {
        var parentByNodeKey = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in nodes)
        {
            var parentKey = node.ParentNodeKey;
            if (!string.IsNullOrWhiteSpace(parentKey) && nodeKeys.Contains(parentKey) && !parentKey.Equals(node.NodeKey, StringComparison.OrdinalIgnoreCase))
            {
                parentByNodeKey[node.NodeKey] = parentKey;
                continue;
            }

            if (fallbackParentByNodeKey.TryGetValue(node.NodeKey, out var fallbackParent) &&
                !fallbackParent.Equals(node.NodeKey, StringComparison.OrdinalIgnoreCase))
            {
                parentByNodeKey[node.NodeKey] = fallbackParent;
                continue;
            }

            parentByNodeKey[node.NodeKey] = null;
        }

        return parentByNodeKey;
    }

    private static Dictionary<string, List<SchematicNodeEntity>> BuildChildrenLookup(
        IEnumerable<SchematicNodeEntity> nodes,
        IReadOnlyDictionary<string, string?> parentByNodeKey)
    {
        var nodeByKey = nodes.ToDictionary(node => node.NodeKey, StringComparer.OrdinalIgnoreCase);
        var childrenByParentKey = new Dictionary<string, List<SchematicNodeEntity>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (nodeKey, parentKey) in parentByNodeKey)
        {
            if (string.IsNullOrWhiteSpace(parentKey))
            {
                continue;
            }

            if (!nodeByKey.TryGetValue(nodeKey, out var node))
            {
                continue;
            }

            if (!childrenByParentKey.TryGetValue(parentKey, out var children))
            {
                children = [];
                childrenByParentKey[parentKey] = children;
            }

            children.Add(node);
        }

        foreach (var children in childrenByParentKey.Values)
        {
            children.Sort(CompareNodesForLayout);
        }

        return childrenByParentKey;
    }

    private static List<SchematicNodeEntity> BuildRootList(
        IEnumerable<SchematicNodeEntity> nodes,
        IReadOnlyDictionary<string, string?> parentByNodeKey)
    {
        return nodes
            .Where(node => !parentByNodeKey.TryGetValue(node.NodeKey, out var parentKey) || string.IsNullOrWhiteSpace(parentKey))
            .OrderBy(node => node, Comparer<SchematicNodeEntity>.Create(CompareNodesForLayout))
            .ToList();
    }

    private static Dictionary<string, int> BuildDepthLookup(
        IEnumerable<SchematicNodeEntity> nodes,
        IReadOnlyList<SchematicNodeEntity> roots,
        IReadOnlyDictionary<string, List<SchematicNodeEntity>> childrenByParentKey)
    {
        var depthByNodeKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<(string NodeKey, int Depth)>();

        foreach (var root in roots)
        {
            queue.Enqueue((root.NodeKey, 0));
        }

        while (queue.Count > 0)
        {
            var (nodeKey, depth) = queue.Dequeue();
            if (depthByNodeKey.TryGetValue(nodeKey, out var existingDepth) && existingDepth <= depth)
            {
                continue;
            }

            depthByNodeKey[nodeKey] = depth;

            if (!childrenByParentKey.TryGetValue(nodeKey, out var children))
            {
                continue;
            }

            foreach (var child in children)
            {
                queue.Enqueue((child.NodeKey, depth + 1));
            }
        }

        var maxAssignedDepth = depthByNodeKey.Values.DefaultIfEmpty(0).Max();
        foreach (var node in nodes.OrderBy(node => node.NodeKey, StringComparer.OrdinalIgnoreCase))
        {
            if (!depthByNodeKey.ContainsKey(node.NodeKey))
            {
                maxAssignedDepth++;
                depthByNodeKey[node.NodeKey] = maxAssignedDepth;
            }
        }

        return depthByNodeKey;
    }

    private static Dictionary<string, double> BuildYLookup(
        IEnumerable<SchematicNodeEntity> nodes,
        IReadOnlyList<SchematicNodeEntity> roots,
        IReadOnlyDictionary<string, List<SchematicNodeEntity>> childrenByParentKey)
    {
        var yIndexByNodeKey = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        double leafCursor = 0;

        foreach (var root in roots)
        {
            AssignSubtreeYIndex(root.NodeKey, childrenByParentKey, yIndexByNodeKey, visiting, ref leafCursor);
            leafCursor += RootSeparationUnits;
        }

        foreach (var node in nodes.OrderBy(node => node.NodeKey, StringComparer.OrdinalIgnoreCase))
        {
            if (!yIndexByNodeKey.ContainsKey(node.NodeKey))
            {
                AssignSubtreeYIndex(node.NodeKey, childrenByParentKey, yIndexByNodeKey, visiting, ref leafCursor);
                leafCursor += 1;
            }
        }

        var maxIndex = yIndexByNodeKey.Values.DefaultIfEmpty(0).Max();
        var yByNodeKey = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var (nodeKey, yIndex) in yIndexByNodeKey)
        {
            if (maxIndex <= 0)
            {
                yByNodeKey[nodeKey] = 0.5;
                continue;
            }

            var ratio = yIndex / maxIndex;
            yByNodeKey[nodeKey] = MinY + ratio * (MaxY - MinY);
        }

        return yByNodeKey;
    }

    private static double AssignSubtreeYIndex(
        string nodeKey,
        IReadOnlyDictionary<string, List<SchematicNodeEntity>> childrenByParentKey,
        IDictionary<string, double> yIndexByNodeKey,
        ISet<string> visiting,
        ref double leafCursor)
    {
        if (yIndexByNodeKey.TryGetValue(nodeKey, out var existingY))
        {
            return existingY;
        }

        if (!visiting.Add(nodeKey))
        {
            var cycleY = leafCursor;
            leafCursor += 1;
            yIndexByNodeKey[nodeKey] = cycleY;
            return cycleY;
        }

        if (!childrenByParentKey.TryGetValue(nodeKey, out var children) || children.Count == 0)
        {
            var leafY = leafCursor;
            leafCursor += 1;
            yIndexByNodeKey[nodeKey] = leafY;
            visiting.Remove(nodeKey);
            return leafY;
        }

        double sum = 0;
        var count = 0;

        foreach (var child in children)
        {
            sum += AssignSubtreeYIndex(child.NodeKey, childrenByParentKey, yIndexByNodeKey, visiting, ref leafCursor);
            count++;
        }

        var y = count == 0 ? leafCursor : sum / count;
        yIndexByNodeKey[nodeKey] = y;
        visiting.Remove(nodeKey);
        return y;
    }

    private static Dictionary<string, double> ApplyDepthSpacing(
        IEnumerable<SchematicNodeEntity> nodes,
        IReadOnlyDictionary<string, int> depthByNodeKey,
        IReadOnlyDictionary<string, double> yByNodeKey)
    {
        var adjustedY = new Dictionary<string, double>(yByNodeKey, StringComparer.OrdinalIgnoreCase);
        var groupedNodeKeys = nodes
            .GroupBy(node => depthByNodeKey.TryGetValue(node.NodeKey, out var depth) ? depth : 0)
            .ToDictionary(group => group.Key, group => group.Select(node => node.NodeKey).ToList());

        foreach (var nodeKeysAtDepth in groupedNodeKeys.Values)
        {
            if (nodeKeysAtDepth.Count == 0)
            {
                continue;
            }

            nodeKeysAtDepth.Sort((left, right) =>
            {
                var leftY = adjustedY.GetValueOrDefault(left, 0.5);
                var rightY = adjustedY.GetValueOrDefault(right, 0.5);
                var yCompare = leftY.CompareTo(rightY);
                return yCompare != 0 ? yCompare : StringComparer.OrdinalIgnoreCase.Compare(left, right);
            });

            if (nodeKeysAtDepth.Count == 1)
            {
                var onlyKey = nodeKeysAtDepth[0];
                adjustedY[onlyKey] = Math.Clamp(adjustedY.GetValueOrDefault(onlyKey, 0.5), MinY, MaxY);
                continue;
            }

            var availableSpan = MaxY - MinY;
            var gap = Math.Max(AbsoluteMinDepthGap, Math.Min(PreferredDepthGap, availableSpan / (nodeKeysAtDepth.Count - 1)));

            var firstKey = nodeKeysAtDepth[0];
            adjustedY[firstKey] = Math.Clamp(adjustedY.GetValueOrDefault(firstKey, MinY), MinY, MaxY);

            for (var i = 1; i < nodeKeysAtDepth.Count; i++)
            {
                var key = nodeKeysAtDepth[i];
                var previousKey = nodeKeysAtDepth[i - 1];
                adjustedY[key] = Math.Max(adjustedY.GetValueOrDefault(key, MinY), adjustedY[previousKey] + gap);
            }

            var lastKey = nodeKeysAtDepth[^1];
            var overflow = adjustedY[lastKey] - MaxY;
            if (overflow > 0)
            {
                foreach (var key in nodeKeysAtDepth)
                {
                    adjustedY[key] -= overflow;
                }
            }

            var underflow = MinY - adjustedY[nodeKeysAtDepth[0]];
            if (underflow > 0)
            {
                foreach (var key in nodeKeysAtDepth)
                {
                    adjustedY[key] += underflow;
                }
            }

            foreach (var key in nodeKeysAtDepth)
            {
                adjustedY[key] = Math.Clamp(adjustedY[key], MinY, MaxY);
            }
        }

        return adjustedY;
    }

    private static Dictionary<string, string> BuildFallbackParentLookup(
        IEnumerable<SchematicEdgeEntity> edges,
        IReadOnlySet<string> nodeKeys)
    {
        return edges
            .Where(edge => nodeKeys.Contains(edge.SourceNodeKey) && nodeKeys.Contains(edge.TargetNodeKey))
            .GroupBy(edge => edge.TargetNodeKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(edge => edge.SourceNodeKey)
                    .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                    .First(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static double ComputeX(int depth, int maxDepth)
    {
        if (maxDepth <= 0)
        {
            return 0.5;
        }

        var ratio = (double)depth / maxDepth;
        var easedRatio = Math.Pow(ratio, 0.9);
        return MinX + easedRatio * (MaxX - MinX);
    }

    private static int CompareNodesForLayout(SchematicNodeEntity? left, SchematicNodeEntity? right)
    {
        if (left is null && right is null)
        {
            return 0;
        }

        if (left is null)
        {
            return -1;
        }

        if (right is null)
        {
            return 1;
        }

        var rankCompare = GetNodeTypeRank(left).CompareTo(GetNodeTypeRank(right));
        if (rankCompare != 0)
        {
            return rankCompare;
        }

        var labelCompare = StringComparer.CurrentCultureIgnoreCase.Compare(left.Label, right.Label);
        if (labelCompare != 0)
        {
            return labelCompare;
        }

        return StringComparer.OrdinalIgnoreCase.Compare(left.NodeKey, right.NodeKey);
    }

    private static int GetNodeTypeRank(SchematicNodeEntity node)
    {
        var nodeType = node.NodeType?.Trim().ToLowerInvariant();
        return nodeType switch
        {
            "grid" or "site" or "building" => 0,
            "transformer" => 1,
            "zone" => 2,
            "subzone" => 3,
            _ => 4
        };
    }

    private static FacilityEntity CloneFacility(
        FacilityEntity source,
        ICollection<SchematicNodeEntity> nodes,
        ICollection<SchematicEdgeEntity> edges)
    {
        return new FacilityEntity
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            TimeZone = source.TimeZone,
            Nodes = nodes,
            Edges = edges
        };
    }

    private static SchematicNodeEntity CloneNode(SchematicNodeEntity source)
    {
        return new SchematicNodeEntity
        {
            Id = source.Id,
            FacilityId = source.FacilityId,
            NodeKey = source.NodeKey,
            Label = source.Label,
            NodeType = source.NodeType,
            Zone = source.Zone,
            MeterUrn = source.MeterUrn,
            ParentNodeKey = source.ParentNodeKey,
            XHint = source.XHint,
            YHint = source.YHint
        };
    }

    private static SchematicEdgeEntity CloneEdge(SchematicEdgeEntity source)
    {
        return new SchematicEdgeEntity
        {
            Id = source.Id,
            FacilityId = source.FacilityId,
            SourceNodeKey = source.SourceNodeKey,
            TargetNodeKey = source.TargetNodeKey
        };
    }
}