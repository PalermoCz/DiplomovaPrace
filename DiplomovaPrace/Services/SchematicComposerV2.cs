using DiplomovaPrace.Persistence.Schematic;

namespace DiplomovaPrace.Services;

// ── V2 Layout DTOs ──────────────────────────────────────────────────────────

public sealed class SchematicLayoutV2
{
    public required IReadOnlyList<SchematicNodeLayoutV2> Nodes { get; init; }
    public required IReadOnlyList<SchematicLinkLayoutV2> Links { get; init; }
    public double CanvasWidth { get; init; }
    public double CanvasHeight { get; init; }
}

public sealed record SchematicNodeLayoutV2(
    string NodeKey,
    string Label,
    string? NodeType,
    string? Zone,
    string? MeterUrn,
    string? ParentNodeKey,
    double X,
    double Y,
    SchematicNodeLayerV2 Layer,
    bool HasChildren);

public sealed record SchematicLinkLayoutV2(
    string SourceNodeKey,
    string TargetNodeKey,
    bool IsParentChild);

public enum SchematicNodeLayerV2
{
    Backbone,
    Utility,
    Zone,
    Distribution,
    Leaf
}

// ── V2 Composer ─────────────────────────────────────────────────────────────

/// <summary>
/// Generic tree-based schematic composer for facility graphs.
/// Produces a hierarchical layout using subtree-width centering:
///   - each parent is centered above its children
///   - subtree width determines horizontal allocation
///   - no hardcoded positions, works for any topology
///
/// Pipeline: data acquisition → graph normalization → tree layout → output
/// </summary>
public static class SchematicComposerV2
{
    private const double LeafSpacing = 68;
    private const double LevelHeight = 100;
    private const double PaddingX = 55;
    private const double PaddingY = 50;
    private const double MinCanvasWidth = 400;
    private const double MinCanvasHeight = 300;

    /// <summary>
    /// Compose a schematic layout from a facility entity.
    /// Filters weather nodes, builds a tree, computes subtree-width layout.
    /// </summary>
    public static SchematicLayoutV2 Compose(FacilityEntity facility)
    {
        ArgumentNullException.ThrowIfNull(facility);

        // ── 1. Data acquisition: deduplicate and filter ──────────────────
        var allNodes = facility.Nodes
            .Where(n => !IsExcludedNode(n.NodeKey))
            .GroupBy(n => n.NodeKey, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        if (allNodes.Count == 0)
            return EmptyLayout();

        // ── 2. Graph normalization: build parent→children tree ───────────
        var nodeByKey = allNodes.ToDictionary(n => n.NodeKey, StringComparer.OrdinalIgnoreCase);
        var childrenMap = new Dictionary<string, List<SchematicNodeEntity>>(StringComparer.OrdinalIgnoreCase);
        var rootNodes = new List<SchematicNodeEntity>();

        foreach (var node in allNodes)
        {
            var parentKey = node.ParentNodeKey;
            if (!string.IsNullOrWhiteSpace(parentKey)
                && nodeByKey.ContainsKey(parentKey)
                && !parentKey.Equals(node.NodeKey, StringComparison.OrdinalIgnoreCase))
            {
                if (!childrenMap.TryGetValue(parentKey, out var children))
                {
                    children = [];
                    childrenMap[parentKey] = children;
                }
                children.Add(node);
            }
            else
            {
                rootNodes.Add(node);
            }
        }

        // Fallback: if no parent info, try to infer from explicit edges
        if (rootNodes.Count == allNodes.Count && facility.Edges.Count > 0)
        {
            InferTreeFromEdges(allNodes, facility.Edges, nodeByKey, childrenMap, rootNodes);
        }

        // ── 3. Schematic composition: calculate subtree widths ───────────
        var subtreeWidth = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        void CalcWidth(string nodeKey)
        {
            if (!childrenMap.TryGetValue(nodeKey, out var children) || children.Count == 0)
            {
                subtreeWidth[nodeKey] = 1.0;
                return;
            }
            foreach (var child in children)
            {
                if (!subtreeWidth.ContainsKey(child.NodeKey))
                    CalcWidth(child.NodeKey);
            }
            subtreeWidth[nodeKey] = children.Sum(c => subtreeWidth.GetValueOrDefault(c.NodeKey, 1.0));
        }

        foreach (var root in rootNodes)
            CalcWidth(root.NodeKey);

        // ── 4. Sort children for balanced visual output ──────────────────
        SortChildrenForLayout(childrenMap, subtreeWidth);
        SortNodeList(rootNodes, subtreeWidth);

        // ── 5. Position nodes using subtree-width centering ──────────────
        var totalRootWidth = rootNodes.Sum(r => subtreeWidth.GetValueOrDefault(r.NodeKey, 1.0));
        var totalPixelWidth = Math.Max(MinCanvasWidth - PaddingX * 2, totalRootWidth * LeafSpacing);

        var positions = new Dictionary<string, (double X, double Y)>(StringComparer.OrdinalIgnoreCase);

        if (rootNodes.Count == 1)
        {
            PositionSubtree(rootNodes[0].NodeKey, totalPixelWidth / 2.0, totalPixelWidth, 0,
                childrenMap, subtreeWidth, positions);
        }
        else
        {
            // Multiple roots: lay them out side by side
            var cursor = 0.0;
            foreach (var root in rootNodes)
            {
                var rootSW = subtreeWidth.GetValueOrDefault(root.NodeKey, 1.0);
                var allocated = (rootSW / totalRootWidth) * totalPixelWidth;
                var center = cursor + allocated / 2.0;
                PositionSubtree(root.NodeKey, center, allocated, 0,
                    childrenMap, subtreeWidth, positions);
                cursor += allocated;
            }
        }

        // ── 6. Normalize coordinates (add padding, compute canvas) ───────
        if (positions.Count == 0)
            return EmptyLayout();

        var allX = positions.Values.Select(p => p.Item1).ToList();
        var allY = positions.Values.Select(p => p.Item2).ToList();
        var minX = allX.Min();
        var maxX = allX.Max();
        var minY = allY.Min();
        var maxY = allY.Max();

        var offsetX = PaddingX - minX;
        var offsetY = PaddingY - minY;
        var canvasWidth = Math.Max(MinCanvasWidth, (maxX - minX) + PaddingX * 2);
        var canvasHeight = Math.Max(MinCanvasHeight, (maxY - minY) + PaddingY * 2);

        // ── 7. Build output layout nodes ─────────────────────────────────
        var layoutNodes = allNodes.Select(n =>
        {
            var pos = positions.GetValueOrDefault(n.NodeKey, (canvasWidth / 2, canvasHeight / 2));
            var hasChildren = childrenMap.TryGetValue(n.NodeKey, out var ch) && ch.Count > 0;
            var layer = ClassifyLayer(n, hasChildren);

            return new SchematicNodeLayoutV2(
                n.NodeKey, n.Label, n.NodeType, n.Zone, n.MeterUrn, n.ParentNodeKey,
                pos.Item1 + offsetX, pos.Item2 + offsetY,
                layer, hasChildren);
        }).ToList();

        // ── 8. Build links ───────────────────────────────────────────────
        var links = BuildLinks(allNodes, facility.Edges, nodeByKey, childrenMap);

        return new SchematicLayoutV2
        {
            Nodes = layoutNodes,
            Links = links,
            CanvasWidth = canvasWidth,
            CanvasHeight = canvasHeight
        };
    }

    // ── Tree positioning ─────────────────────────────────────────────────────

    private static void PositionSubtree(
        string nodeKey,
        double xCenter,
        double allocatedWidth,
        int depth,
        Dictionary<string, List<SchematicNodeEntity>> childrenMap,
        Dictionary<string, double> subtreeWidth,
        Dictionary<string, (double X, double Y)> positions)
    {
        positions[nodeKey] = (xCenter, depth * LevelHeight);

        if (!childrenMap.TryGetValue(nodeKey, out var children) || children.Count == 0)
            return;

        var totalChildSW = children.Sum(c => subtreeWidth.GetValueOrDefault(c.NodeKey, 1.0));
        if (totalChildSW <= 0) totalChildSW = 1.0;

        var minChildPixelWidth = totalChildSW * LeafSpacing;
        var effectiveWidth = Math.Max(allocatedWidth, minChildPixelWidth);

        var startX = xCenter - effectiveWidth / 2.0;
        var cursor = startX;

        foreach (var child in children)
        {
            var childSW = subtreeWidth.GetValueOrDefault(child.NodeKey, 1.0);
            var childAllocated = (childSW / totalChildSW) * effectiveWidth;
            var childCenter = cursor + childAllocated / 2.0;

            PositionSubtree(child.NodeKey, childCenter, childAllocated, depth + 1,
                childrenMap, subtreeWidth, positions);

            cursor += childAllocated;
        }
    }

    // ── Child sorting ────────────────────────────────────────────────────────

    private static void SortChildrenForLayout(
        Dictionary<string, List<SchematicNodeEntity>> childrenMap,
        Dictionary<string, double> subtreeWidth)
    {
        foreach (var children in childrenMap.Values)
        {
            SortNodeList(children, subtreeWidth);
        }
    }

    /// <summary>
    /// Sort sibling nodes for visual balance.
    /// Uses x_hint from CSV if available (preserves imported spatial arrangement),
    /// otherwise sorts by subtree width (larger subtrees get more central placement).
    /// </summary>
    private static void SortNodeList(
        List<SchematicNodeEntity> nodes,
        Dictionary<string, double> subtreeWidth)
    {
        if (nodes.Count <= 1) return;

        // If most nodes have x_hints, use them for ordering
        var hintCount = nodes.Count(n => n.XHint.HasValue);
        if (hintCount > nodes.Count / 2)
        {
            nodes.Sort((a, b) =>
            {
                var xa = a.XHint ?? 0.5;
                var xb = b.XHint ?? 0.5;
                var cmp = xa.CompareTo(xb);
                return cmp != 0 ? cmp : StringComparer.OrdinalIgnoreCase.Compare(a.Label, b.Label);
            });
        }
        else
        {
            // Interleave large and small subtrees for visual balance:
            // Sort by subtree width descending, then place alternately left/right
            var sorted = nodes
                .OrderByDescending(n => subtreeWidth.GetValueOrDefault(n.NodeKey, 1.0))
                .ThenBy(n => n.Label, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var result = new SchematicNodeEntity[sorted.Count];
            int left = 0, right = sorted.Count - 1;
            for (int i = 0; i < sorted.Count; i++)
            {
                if (i % 2 == 0)
                    result[left++] = sorted[i];
                else
                    result[right--] = sorted[i];
            }

            nodes.Clear();
            nodes.AddRange(result);
        }
    }

    // ── Edge-based tree inference ────────────────────────────────────────────

    private static void InferTreeFromEdges(
        List<SchematicNodeEntity> allNodes,
        ICollection<SchematicEdgeEntity> edges,
        Dictionary<string, SchematicNodeEntity> nodeByKey,
        Dictionary<string, List<SchematicNodeEntity>> childrenMap,
        List<SchematicNodeEntity> rootNodes)
    {
        // Build adjacency from edges, try to find root via in-degree
        var parentLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var edge in edges)
        {
            if (!nodeByKey.ContainsKey(edge.SourceNodeKey) || !nodeByKey.ContainsKey(edge.TargetNodeKey))
                continue;
            if (edge.SourceNodeKey.Equals(edge.TargetNodeKey, StringComparison.OrdinalIgnoreCase))
                continue;

            // source → target means source is parent of target
            parentLookup.TryAdd(edge.TargetNodeKey, edge.SourceNodeKey);
        }

        if (parentLookup.Count == 0)
            return;

        childrenMap.Clear();
        rootNodes.Clear();

        foreach (var node in allNodes)
        {
            if (parentLookup.TryGetValue(node.NodeKey, out var parentKey))
            {
                if (!childrenMap.TryGetValue(parentKey, out var children))
                {
                    children = [];
                    childrenMap[parentKey] = children;
                }
                children.Add(node);
            }
            else
            {
                rootNodes.Add(node);
            }
        }
    }

    // ── Node classification ──────────────────────────────────────────────────

    private static SchematicNodeLayerV2 ClassifyLayer(SchematicNodeEntity node, bool hasChildren)
    {
        var type = node.NodeType?.Trim().ToLowerInvariant();
        return type switch
        {
            "grid" or "site" or "building" or "transformer" => SchematicNodeLayerV2.Backbone,
            "generator_pv" or "generator_chp" => SchematicNodeLayerV2.Utility,
            "utility_cooling" or "utility_heating" => SchematicNodeLayerV2.Utility,
            "zone" => SchematicNodeLayerV2.Zone,
            "subzone" or "distribution" or "branch" => SchematicNodeLayerV2.Distribution,
            _ when type is not null && type.EndsWith("_branch") => SchematicNodeLayerV2.Distribution,
            _ => hasChildren ? SchematicNodeLayerV2.Distribution : SchematicNodeLayerV2.Leaf
        };
    }

    private static bool IsExcludedNode(string nodeKey)
    {
        return string.Equals(nodeKey, "weather_main", StringComparison.OrdinalIgnoreCase);
    }

    // ── Link building ────────────────────────────────────────────────────────

    private static List<SchematicLinkLayoutV2> BuildLinks(
        IReadOnlyList<SchematicNodeEntity> nodes,
        ICollection<SchematicEdgeEntity> explicitEdges,
        Dictionary<string, SchematicNodeEntity> nodeByKey,
        Dictionary<string, List<SchematicNodeEntity>> childrenMap)
    {
        var links = new List<SchematicLinkLayoutV2>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Parent-child links (primary)
        foreach (var (parentKey, children) in childrenMap)
        {
            if (!nodeByKey.ContainsKey(parentKey))
                continue;
            foreach (var child in children)
            {
                if (!nodeByKey.ContainsKey(child.NodeKey))
                    continue;
                var key = $"{parentKey}|{child.NodeKey}";
                if (seen.Add(key))
                    links.Add(new SchematicLinkLayoutV2(parentKey, child.NodeKey, IsParentChild: true));
            }
        }

        // Explicit edges not covered by parent-child
        foreach (var edge in explicitEdges)
        {
            if (!nodeByKey.ContainsKey(edge.SourceNodeKey) || !nodeByKey.ContainsKey(edge.TargetNodeKey))
                continue;
            if (IsExcludedNode(edge.SourceNodeKey) || IsExcludedNode(edge.TargetNodeKey))
                continue;
            var key1 = $"{edge.SourceNodeKey}|{edge.TargetNodeKey}";
            var key2 = $"{edge.TargetNodeKey}|{edge.SourceNodeKey}";
            if (seen.Contains(key1) || seen.Contains(key2))
                continue;
            if (seen.Add(key1))
                links.Add(new SchematicLinkLayoutV2(edge.SourceNodeKey, edge.TargetNodeKey, IsParentChild: false));
        }

        return links;
    }

    // ── Empty layout ─────────────────────────────────────────────────────────

    private static SchematicLayoutV2 EmptyLayout() => new()
    {
        Nodes = [],
        Links = [],
        CanvasWidth = MinCanvasWidth,
        CanvasHeight = MinCanvasHeight
    };
}
