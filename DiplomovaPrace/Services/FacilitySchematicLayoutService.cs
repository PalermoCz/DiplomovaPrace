using DiplomovaPrace.Persistence.Schematic;

namespace DiplomovaPrace.Services;

/// <summary>
/// Deterministicky pripravuje schematic layout facility grafu ve stylu metering schematu:
/// citelna backbone vrstva nahore, utility/generation vrstva pod backbone
/// a kompaktni area bloky v jednotne schematicke sekvenci.
/// </summary>
public static class FacilitySchematicLayoutService
{
    private const double MinX = 0.04;
    private const double MaxX = 0.965;
    private const double MinY = 0.06;
    private const double MaxY = 0.95;

    private const double BackboneY = 0.145;
    private const double UtilityY = 0.265;

    private const double AreaTopStart = 0.400;
    private const double AreaRowGap = 0.024;
    private const int DefaultAreaEndpointColumns = 3;

    private static readonly string[] PreferredAreaOrder =
    [
        "emission_lab",
        "design_studio",
        "workshop",
        "offices"
    ];

    private static readonly Dictionary<string, double> PreferredUtilityXByNodeKey = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cooling_main"] = 0.15,
        ["heating_main"] = 0.27,
        ["chp_main"] = 0.40,
        ["pv_main"] = 0.56
    };

    public static FacilityEntity BuildReadableLayout(FacilityEntity source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var sourceNodes = source.Nodes
            .GroupBy(node => node.NodeKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        var sourceEdges = source.Edges.ToList();

        if (sourceNodes.Count == 0)
        {
            return CloneFacility(source, [], sourceEdges.Select(CloneEdge).ToList());
        }

        var nodeByKey = sourceNodes.ToDictionary(node => node.NodeKey, StringComparer.OrdinalIgnoreCase);
        var nodeKeys = nodeByKey.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var fallbackParentByNodeKey = BuildFallbackParentLookup(sourceEdges, nodeKeys);
        var parentByNodeKey = BuildParentLookup(nodeByKey.Values, nodeKeys, fallbackParentByNodeKey);
        var childrenByParentKey = BuildChildrenLookup(nodeByKey.Values, parentByNodeKey);
        var areaByNodeKey = BuildAreaLookup(nodeByKey.Values, parentByNodeKey, childrenByParentKey);
        var coordinatesByNodeKey = BuildSchematicCoordinates(nodeByKey.Values, parentByNodeKey, childrenByParentKey, areaByNodeKey);

        var positionedNodes = nodeByKey.Values
            .Select(node =>
            {
                var clone = CloneNode(node);
                if (coordinatesByNodeKey.TryGetValue(node.NodeKey, out var point))
                {
                    clone.XHint = point.X;
                    clone.YHint = point.Y;
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

    private static Dictionary<string, LayoutPoint> BuildSchematicCoordinates(
        IEnumerable<SchematicNodeEntity> nodes,
        IReadOnlyDictionary<string, string?> parentByNodeKey,
        IReadOnlyDictionary<string, List<SchematicNodeEntity>> childrenByParentKey,
        IReadOnlyDictionary<string, string> areaByNodeKey)
    {
        var nodeList = nodes.ToList();
        var coordinatesByNodeKey = new Dictionary<string, LayoutPoint>(StringComparer.OrdinalIgnoreCase);

        PlaceBackboneRow(nodeList, coordinatesByNodeKey);
        PlaceUtilityGenerationRow(nodeList, areaByNodeKey, coordinatesByNodeKey);
        PlaceAreaBlockStack(nodeList, parentByNodeKey, childrenByParentKey, areaByNodeKey, coordinatesByNodeKey);

        PlaceRemainingNodes(nodeList, parentByNodeKey, childrenByParentKey, coordinatesByNodeKey);
        ResolveCoordinateCollisions(nodeList, coordinatesByNodeKey);

        return coordinatesByNodeKey;
    }

    private static void PlaceBackboneRow(
        IReadOnlyList<SchematicNodeEntity> nodes,
        IDictionary<string, LayoutPoint> coordinatesByNodeKey)
    {
        var backboneNodes = nodes
            .Where(IsGridBackboneNode)
            .OrderBy(node => GetNodeSortRank(node))
            .ThenBy(node => node.Label, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(node => node.NodeKey, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (backboneNodes.Count == 0)
        {
            return;
        }

        var rootBackboneNode = backboneNodes
            .FirstOrDefault(node => node.NodeKey.Equals("ext_grid", StringComparison.OrdinalIgnoreCase))
            ?? backboneNodes.FirstOrDefault(node => string.Equals(node.NodeType, "grid", StringComparison.OrdinalIgnoreCase)
                || string.Equals(node.NodeType, "site", StringComparison.OrdinalIgnoreCase)
                || string.Equals(node.NodeType, "building", StringComparison.OrdinalIgnoreCase));

        if (rootBackboneNode is not null)
        {
            coordinatesByNodeKey[rootBackboneNode.NodeKey] = new LayoutPoint(0.08, BackboneY);
        }

        var secondaryBackboneNodes = backboneNodes
            .Where(node => !string.Equals(node.NodeType, "transformer", StringComparison.OrdinalIgnoreCase))
            .Where(node => rootBackboneNode is null || !node.NodeKey.Equals(rootBackboneNode.NodeKey, StringComparison.OrdinalIgnoreCase))
            .ToList();

        PlaceLinearRow(secondaryBackboneNodes, coordinatesByNodeKey, startX: 0.20, step: 0.13, y: BackboneY, minX: 0.16, maxX: 0.44);

        var transformerNodes = backboneNodes
            .Where(node => string.Equals(node.NodeType, "transformer", StringComparison.OrdinalIgnoreCase))
            .OrderBy(GetTransformerSortRank)
            .ThenBy(node => node.Label, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(node => node.NodeKey, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (transformerNodes.Count > 0)
        {
            var transformerStartX = 0.22;
            var transformerEndX = 0.84;
            var availableWidth = Math.Max(0.08, transformerEndX - transformerStartX);
            var transformerStep = transformerNodes.Count <= 1
                ? 0d
                : Math.Max(0.18, availableWidth / (transformerNodes.Count - 1));

            for (var index = 0; index < transformerNodes.Count; index++)
            {
                var x = transformerNodes.Count <= 1
                    ? (transformerStartX + transformerEndX) / 2d
                    : Math.Clamp(transformerStartX + (index * transformerStep), transformerStartX, transformerEndX);
                coordinatesByNodeKey[transformerNodes[index].NodeKey] = new LayoutPoint(x, BackboneY);
            }
        }
    }

    private static void PlaceUtilityGenerationRow(
        IReadOnlyList<SchematicNodeEntity> nodes,
        IReadOnlyDictionary<string, string> areaByNodeKey,
        IDictionary<string, LayoutPoint> coordinatesByNodeKey)
    {
        var utilityAndGenerationNodes = nodes
            .Where(node => IsGenerationNode(node) || IsUtilityNode(node))
            .OrderBy(GetGenerationUtilitySortRank)
            .ThenBy(node => node.Label, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(node => node.NodeKey, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var node in utilityAndGenerationNodes)
        {
            if (coordinatesByNodeKey.ContainsKey(node.NodeKey))
            {
                continue;
            }

            if (PreferredUtilityXByNodeKey.TryGetValue(node.NodeKey, out var preferredX))
            {
                coordinatesByNodeKey[node.NodeKey] = new LayoutPoint(Math.Clamp(preferredX, MinX, MaxX), UtilityY);
            }
        }

        var remainingUtilityNodes = utilityAndGenerationNodes
            .Where(node => !coordinatesByNodeKey.ContainsKey(node.NodeKey))
            .ToList();

        if (remainingUtilityNodes.Count > 0)
        {
            PlaceCenteredRow(remainingUtilityNodes, coordinatesByNodeKey, centerX: 0.70, step: 0.11, y: UtilityY, minX: 0.50, maxX: 0.90);
        }

        var utilityBranchOtherNodes = nodes
            .Where(node => !coordinatesByNodeKey.ContainsKey(node.NodeKey))
            .Where(node => string.Equals(areaByNodeKey.GetValueOrDefault(node.NodeKey), "utility_branches", StringComparison.OrdinalIgnoreCase))
            .OrderBy(node => GetNodeSortRank(node))
            .ThenBy(node => node.Label, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(node => node.NodeKey, StringComparer.OrdinalIgnoreCase)
            .ToList();

        PlaceCenteredRow(utilityBranchOtherNodes, coordinatesByNodeKey, centerX: 0.76, step: 0.09, y: UtilityY + 0.045, minX: 0.60, maxX: 0.93);
    }

    private static void PlaceAreaBlockStack(
        IReadOnlyList<SchematicNodeEntity> nodes,
        IReadOnlyDictionary<string, string?> parentByNodeKey,
        IReadOnlyDictionary<string, List<SchematicNodeEntity>> childrenByParentKey,
        IReadOnlyDictionary<string, string> areaByNodeKey,
        IDictionary<string, LayoutPoint> coordinatesByNodeKey)
    {
        var orderedAreaKeys = areaByNodeKey.Values
            .Where(area => !string.Equals(area, "backbone", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(area, "utility_branches", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(GetAreaSortRank)
            .ThenBy(area => area, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var areaLayouts = new List<(string AreaKey, AreaNodeGroups Groups, double Height)>();

        foreach (var areaKey in orderedAreaKeys)
        {
            var areaNodesForSizing = nodes
                .Where(node => string.Equals(areaByNodeKey.GetValueOrDefault(node.NodeKey), areaKey, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (areaNodesForSizing.Count == 0)
            {
                continue;
            }

            var grouped = GroupAreaNodes(areaNodesForSizing, areaKey, parentByNodeKey, childrenByParentKey, areaByNodeKey);
            var endpointColumns = GetAreaEndpointColumns(areaKey, grouped.EndpointNodes.Count);
            var endpointRows = Math.Max(1, (int)Math.Ceiling(grouped.EndpointNodes.Count / (double)endpointColumns));
            var distributionRows = Math.Max(1, (int)Math.Ceiling(grouped.DistributionNodes.Count / 3d));
            var height = CalculateAreaHeight(endpointRows, distributionRows, grouped.AnchorNodes.Count);

            areaLayouts.Add((areaKey, grouped, height));
        }

        if (areaLayouts.Count == 0)
        {
            return;
        }

        var availableHeight = (MaxY - 0.02) - AreaTopStart;
        var gapTotal = Math.Max(0, (areaLayouts.Count - 1) * AreaRowGap);
        var requestedHeight = areaLayouts.Sum(item => item.Height) + gapTotal;

        var heightScale = requestedHeight > availableHeight && requestedHeight > 0
            ? Math.Max(0.76, (availableHeight - gapTotal) / Math.Max(0.01, areaLayouts.Sum(item => item.Height)))
            : 1d;

        var cursorTop = AreaTopStart;

        foreach (var areaLayout in areaLayouts)
        {
            var areaKey = areaLayout.AreaKey;
            var groupedNodes = areaLayout.Groups;
            var areaNodes = nodes
                .Where(node => string.Equals(areaByNodeKey.GetValueOrDefault(node.NodeKey), areaKey, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var height = Math.Max(0.095, areaLayout.Height * heightScale);
            if (cursorTop + height > MaxY - 0.02)
            {
                cursorTop = Math.Max(AreaTopStart, MaxY - 0.02 - height);
            }

            var areaTop = cursorTop;
            var areaBottom = Math.Clamp(areaTop + height, MinY + 0.08, MaxY);
            var areaCenterY = areaTop + ((areaBottom - areaTop) / 2d);

            var areaShift = GetAreaHorizontalShift(areaKey);
            var (baseLeft, baseRight) = GetAreaHorizontalBounds(areaKey);
            var areaLeft = Math.Clamp(baseLeft + areaShift, MinX + 0.02, MaxX - 0.42);
            var areaRight = Math.Clamp(baseRight + (areaShift * 0.35), areaLeft + 0.34, MaxX);
            var anchorX = Math.Clamp(areaLeft + 0.045, MinX, MaxX);

            PlaceAreaAnchors(groupedNodes.AnchorNodes, coordinatesByNodeKey, anchorX, areaTop, areaBottom, areaCenterY);

            var distributionNodes = groupedNodes.DistributionNodes;
            if (distributionNodes.Count == 0 && groupedNodes.EndpointNodes.Count > 0)
            {
                distributionNodes = [groupedNodes.EndpointNodes[0]];
                groupedNodes.EndpointNodes.RemoveAt(0);
            }

            PlaceAreaDistributionNodes(distributionNodes, coordinatesByNodeKey, areaLeft, areaRight, areaTop, areaBottom);

            var endpointColumns = GetAreaEndpointColumns(areaKey, groupedNodes.EndpointNodes.Count);
            PlaceAreaEndpointRows(groupedNodes.EndpointNodes, coordinatesByNodeKey, areaKey, areaLeft, areaRight, areaTop, areaBottom, endpointColumns);

            var unplacedNodes = areaNodes
                .Where(node => !coordinatesByNodeKey.ContainsKey(node.NodeKey))
                .OrderBy(node => node.Label, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(node => node.NodeKey, StringComparer.OrdinalIgnoreCase)
                .ToList();

            for (var index = 0; index < unplacedNodes.Count; index++)
            {
                var y = Math.Clamp(areaTop + 0.062 + (index * 0.032), areaTop + 0.046, areaBottom - 0.03);
                var x = Math.Clamp(areaRight - 0.022, areaLeft + 0.24, areaRight);
                coordinatesByNodeKey[unplacedNodes[index].NodeKey] = new LayoutPoint(x, y);
            }

            cursorTop = areaBottom + AreaRowGap;
        }
    }

    private static AreaNodeGroups GroupAreaNodes(
        IReadOnlyList<SchematicNodeEntity> areaNodes,
        string areaKey,
        IReadOnlyDictionary<string, string?> parentByNodeKey,
        IReadOnlyDictionary<string, List<SchematicNodeEntity>> childrenByParentKey,
        IReadOnlyDictionary<string, string> areaByNodeKey)
    {
        var anchors = areaNodes
            .Where(IsAreaAnchorNode)
            .OrderBy(node => node.Label, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(node => node.NodeKey, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (anchors.Count == 0)
        {
            anchors = areaNodes
                .Where(node =>
                {
                    if (!parentByNodeKey.TryGetValue(node.NodeKey, out var parentNodeKey) || string.IsNullOrWhiteSpace(parentNodeKey))
                    {
                        return true;
                    }

                    var parentArea = areaByNodeKey.GetValueOrDefault(parentNodeKey);
                    return !string.Equals(parentArea, areaKey, StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(node => GetNodeSortRank(node))
                .ThenBy(node => node.Label, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(node => node.NodeKey, StringComparer.OrdinalIgnoreCase)
                .Take(1)
                .ToList();
        }

        var areaNodeKeys = areaNodes
            .Select(node => node.NodeKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var anchorNodeKeys = anchors
            .Select(node => node.NodeKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var areaLocalDepth = BuildAreaLocalDepth(areaNodes, anchors, childrenByParentKey);

        var distributionNodes = areaNodes
            .Where(node => !anchorNodeKeys.Contains(node.NodeKey))
            .Where(node =>
            {
                var localDepth = areaLocalDepth.GetValueOrDefault(node.NodeKey, int.MaxValue);
                if (IsDistributionNodeType(node))
                {
                    return true;
                }

                if (HasAreaChildren(node.NodeKey, areaKey, childrenByParentKey, areaByNodeKey))
                {
                    return localDepth <= 2;
                }

                if (!parentByNodeKey.TryGetValue(node.NodeKey, out var parentNodeKey) || string.IsNullOrWhiteSpace(parentNodeKey))
                {
                    return localDepth <= 1;
                }

                return !areaNodeKeys.Contains(parentNodeKey) && localDepth <= 2;
            })
            .OrderBy(node => areaLocalDepth.GetValueOrDefault(node.NodeKey, int.MaxValue))
            .ThenBy(node => GetNodeSortRank(node))
            .ThenBy(node => node.Label, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(node => node.NodeKey, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var distributionNodeKeys = distributionNodes
            .Select(node => node.NodeKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var endpointNodes = areaNodes
            .Where(node => !anchorNodeKeys.Contains(node.NodeKey) && !distributionNodeKeys.Contains(node.NodeKey))
            .OrderBy(node => areaLocalDepth.GetValueOrDefault(node.NodeKey, int.MaxValue))
            .ThenBy(node => GetNodeSortRank(node))
            .ThenBy(node => node.Label, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(node => node.NodeKey, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new AreaNodeGroups(anchors, distributionNodes, endpointNodes);
    }

    private static void PlaceAreaAnchors(
        IReadOnlyList<SchematicNodeEntity> anchors,
        IDictionary<string, LayoutPoint> coordinatesByNodeKey,
        double x,
        double areaTop,
        double areaBottom,
        double areaCenterY)
    {
        if (anchors.Count == 0)
        {
            return;
        }

        if (anchors.Count == 1)
        {
            var anchorY = ClampWithin(areaTop + ((areaBottom - areaTop) * 0.30), areaTop + 0.035, areaBottom - 0.05);
            coordinatesByNodeKey[anchors[0].NodeKey] = new LayoutPoint(x, anchorY);
            return;
        }

        var gap = Math.Min(0.043, Math.Max(0.024, (areaBottom - areaTop - 0.08) / Math.Max(1, anchors.Count - 1)));
        var startY = ClampWithin(areaTop + 0.04, areaTop + 0.03, areaBottom - 0.09);

        for (var index = 0; index < anchors.Count; index++)
        {
            var y = ClampWithin(startY + (index * gap), areaTop + 0.03, areaBottom - 0.045);
            coordinatesByNodeKey[anchors[index].NodeKey] = new LayoutPoint(x, y);
        }
    }

    private static void PlaceAreaDistributionNodes(
        IReadOnlyList<SchematicNodeEntity> distributionNodes,
        IDictionary<string, LayoutPoint> coordinatesByNodeKey,
        double areaLeft,
        double areaRight,
        double areaTop,
        double areaBottom)
    {
        if (distributionNodes.Count == 0)
        {
            return;
        }

        var visibleNodes = distributionNodes
            .Take(4)
            .ToList();

        var branchLayerY = ClampWithin(areaTop + 0.045, areaTop + 0.036, areaBottom - 0.08);
        var centerX = ClampWithin(areaLeft + ((areaRight - areaLeft) * 0.42), areaLeft + 0.12, areaRight - 0.18);
        var spread = Math.Min(0.18, Math.Max(0.08, (visibleNodes.Count - 1) * 0.04));
        var startX = centerX - (spread / 2d);
        var stepX = visibleNodes.Count <= 1 ? 0d : spread / (visibleNodes.Count - 1);

        for (var index = 0; index < visibleNodes.Count; index++)
        {
            var x = ClampWithin(startX + (index * stepX), areaLeft + 0.18, areaRight - 0.2);
            coordinatesByNodeKey[visibleNodes[index].NodeKey] = new LayoutPoint(x, branchLayerY);
        }
    }

    private static void PlaceAreaEndpointRows(
        IReadOnlyList<SchematicNodeEntity> endpoints,
        IDictionary<string, LayoutPoint> coordinatesByNodeKey,
        string areaKey,
        double areaLeft,
        double areaRight,
        double areaTop,
        double areaBottom,
        int columns)
    {
        if (endpoints.Count == 0)
        {
            return;
        }

        var endpointColumns = Math.Clamp(columns, 1, 5);
        var endpointStartX = ClampWithin(areaLeft + 0.33, areaLeft + 0.24, areaRight - 0.12);
        var endpointEndX = Math.Max(endpointStartX, Math.Min(areaRight, Math.Max(areaRight - 0.02, endpointStartX + 0.04)));
        var colGap = endpointColumns <= 1
            ? 0d
            : (endpointEndX - endpointStartX) / (endpointColumns - 1);
        var rowGap = Math.Clamp(0.038 - (endpointColumns * 0.002), 0.028, 0.04);
        var areaBias = string.Equals(areaKey, "offices", StringComparison.OrdinalIgnoreCase) ? 0.01 : 0;
        var startY = ClampWithin(areaTop + 0.086 + areaBias, areaTop + 0.062, areaBottom - 0.055);

        for (var index = 0; index < endpoints.Count; index++)
        {
            var row = index / endpointColumns;
            var col = index % endpointColumns;

            var x = ClampWithin(endpointStartX + (col * colGap), areaLeft + 0.22, areaRight - 0.02);
            var y = ClampWithin(startY + (row * rowGap), areaTop + 0.062, areaBottom - 0.025);
            coordinatesByNodeKey[endpoints[index].NodeKey] = new LayoutPoint(x, y);
        }
    }

    private static double ClampWithin(double value, double boundA, double boundB)
    {
        var min = Math.Min(boundA, boundB);
        var max = Math.Max(boundA, boundB);
        return Math.Clamp(value, min, max);
    }

    private static double CalculateAreaHeight(int endpointRows, int distributionRows, int anchorCount)
    {
        var rawHeight = 0.072
            + (Math.Max(1, anchorCount) * 0.011)
            + (distributionRows * 0.018)
            + (endpointRows * 0.030);
        return Math.Clamp(rawHeight, 0.095, 0.15);
    }

    private static double GetAreaHorizontalShift(string areaKey)
    {
        return areaKey.ToLowerInvariant() switch
        {
            "emission_lab" => -0.03,
            "design_studio" => -0.008,
            "workshop" => 0.02,
            "offices" => 0.064,
            _ => 0
        };
    }

    private static (double Left, double Right) GetAreaHorizontalBounds(string areaKey)
    {
        return areaKey.ToLowerInvariant() switch
        {
            "emission_lab" => (0.14, 0.88),
            "design_studio" => (0.14, 0.90),
            "workshop" => (0.12, 0.92),
            "offices" => (0.52, 0.92),
            _ => (0.18, 0.92)
        };
    }

    private static int GetAreaEndpointColumns(string areaKey, int endpointCount)
    {
        if (endpointCount <= 2)
        {
            return Math.Max(1, endpointCount);
        }

        if (string.Equals(areaKey, "offices", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Min(2, endpointCount);
        }

        if (string.Equals(areaKey, "workshop", StringComparison.OrdinalIgnoreCase) && endpointCount >= 8)
        {
            return 4;
        }

        if (endpointCount >= 10)
        {
            return 4;
        }

        if (endpointCount >= 5)
        {
            return 3;
        }

        return DefaultAreaEndpointColumns;
    }

    private static Dictionary<string, int> BuildAreaLocalDepth(
        IReadOnlyList<SchematicNodeEntity> areaNodes,
        IReadOnlyList<SchematicNodeEntity> anchors,
        IReadOnlyDictionary<string, List<SchematicNodeEntity>> childrenByParentKey)
    {
        var areaNodeKeys = areaNodes
            .Select(node => node.NodeKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var depthByNodeKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<(string NodeKey, int Depth)>();

        foreach (var anchor in anchors)
        {
            queue.Enqueue((anchor.NodeKey, 0));
        }

        while (queue.Count > 0)
        {
            var (nodeKey, depth) = queue.Dequeue();
            if (depthByNodeKey.TryGetValue(nodeKey, out var existingDepth) && existingDepth <= depth)
            {
                continue;
            }

            depthByNodeKey[nodeKey] = depth;
            if (!childrenByParentKey.TryGetValue(nodeKey, out var children) || children.Count == 0)
            {
                continue;
            }

            foreach (var child in children)
            {
                if (areaNodeKeys.Contains(child.NodeKey))
                {
                    queue.Enqueue((child.NodeKey, depth + 1));
                }
            }
        }

        var maxDepth = depthByNodeKey.Values.DefaultIfEmpty(0).Max();
        foreach (var node in areaNodes)
        {
            if (!depthByNodeKey.ContainsKey(node.NodeKey))
            {
                maxDepth++;
                depthByNodeKey[node.NodeKey] = maxDepth;
            }
        }

        return depthByNodeKey;
    }

    private static bool IsDistributionNodeType(SchematicNodeEntity node)
    {
        var type = node.NodeType?.Trim();
        if (string.IsNullOrWhiteSpace(type))
        {
            return false;
        }

        return type.Equals("subzone", StringComparison.OrdinalIgnoreCase)
            || type.Equals("distribution", StringComparison.OrdinalIgnoreCase)
            || type.Equals("branch", StringComparison.OrdinalIgnoreCase)
            || type.EndsWith("_branch", StringComparison.OrdinalIgnoreCase);
    }

    private static void PlaceRemainingNodes(
        IReadOnlyList<SchematicNodeEntity> nodes,
        IReadOnlyDictionary<string, string?> parentByNodeKey,
        IReadOnlyDictionary<string, List<SchematicNodeEntity>> childrenByParentKey,
        IDictionary<string, LayoutPoint> coordinatesByNodeKey)
    {
        var fallbackCursor = 0;
        var fallbackRailY = MaxY - 0.025;

        foreach (var node in nodes.OrderBy(GetNodeSortRank).ThenBy(node => node.NodeKey, StringComparer.OrdinalIgnoreCase))
        {
            if (coordinatesByNodeKey.ContainsKey(node.NodeKey))
            {
                continue;
            }

            if (parentByNodeKey.TryGetValue(node.NodeKey, out var parentKey)
                && !string.IsNullOrWhiteSpace(parentKey)
                && coordinatesByNodeKey.TryGetValue(parentKey, out var parentPoint))
            {
                var siblingIndex = 0;
                var siblingCount = 1;

                if (childrenByParentKey.TryGetValue(parentKey, out var siblings) && siblings.Count > 0)
                {
                    siblingCount = siblings.Count;
                    siblingIndex = siblings.FindIndex(sibling => sibling.NodeKey.Equals(node.NodeKey, StringComparison.OrdinalIgnoreCase));
                    if (siblingIndex < 0)
                    {
                        siblingIndex = 0;
                    }
                }

                var offset = siblingCount <= 1
                    ? 0d
                    : (siblingIndex - ((siblingCount - 1) / 2d)) * 0.032;

                var column = siblingCount <= 1 ? 0 : siblingIndex % 2;
                var xOffset = 0.082 + (column * 0.026);

                coordinatesByNodeKey[node.NodeKey] = new LayoutPoint(
                    Math.Clamp(parentPoint.X + xOffset, MinX, MaxX),
                    Math.Clamp(parentPoint.Y + offset, MinY, MaxY));
                continue;
            }

            var row = fallbackCursor / 6;
            var col = fallbackCursor % 6;
            var fallbackX = 0.14 + (col * 0.12);
            var fallbackY = fallbackRailY - (row * 0.042);
            fallbackCursor++;
            coordinatesByNodeKey[node.NodeKey] = new LayoutPoint(Math.Clamp(fallbackX, MinX, MaxX), Math.Clamp(fallbackY, MinY, MaxY));
        }

        foreach (var node in nodes)
        {
            if (!coordinatesByNodeKey.ContainsKey(node.NodeKey))
            {
                coordinatesByNodeKey[node.NodeKey] = new LayoutPoint(0.5, 0.5);
            }
        }
    }

    private static void ResolveCoordinateCollisions(
        IReadOnlyList<SchematicNodeEntity> nodes,
        IDictionary<string, LayoutPoint> coordinatesByNodeKey)
    {
        var occupied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in nodes.OrderBy(GetNodeSortRank).ThenBy(node => node.NodeKey, StringComparer.OrdinalIgnoreCase))
        {
            if (!coordinatesByNodeKey.TryGetValue(node.NodeKey, out var point))
            {
                continue;
            }

            var x = Math.Clamp(point.X, MinX, MaxX);
            var y = Math.Clamp(point.Y, MinY, MaxY);
            var key = BuildCoordinateKey(x, y);

            var attempt = 0;
            while (!occupied.Add(key) && attempt < 30)
            {
                attempt++;
                y = Math.Clamp(y + 0.016, MinY, MaxY);
                if (attempt % 6 == 0)
                {
                    x = Math.Clamp(x + 0.014, MinX, MaxX);
                }

                key = BuildCoordinateKey(x, y);
            }

            coordinatesByNodeKey[node.NodeKey] = new LayoutPoint(x, y);
        }
    }

    private static string BuildCoordinateKey(double x, double y)
    {
        return $"{x:0.0000}|{y:0.0000}";
    }

    private static void PlaceLinearRow(
        IReadOnlyList<SchematicNodeEntity> nodes,
        IDictionary<string, LayoutPoint> coordinatesByNodeKey,
        double startX,
        double step,
        double y,
        double minX,
        double maxX)
    {
        for (var index = 0; index < nodes.Count; index++)
        {
            var node = nodes[index];
            if (coordinatesByNodeKey.ContainsKey(node.NodeKey))
            {
                continue;
            }

            var x = Math.Clamp(startX + (index * step), minX, maxX);
            coordinatesByNodeKey[node.NodeKey] = new LayoutPoint(x, Math.Clamp(y, MinY, MaxY));
        }
    }

    private static void PlaceCenteredRow(
        IReadOnlyList<SchematicNodeEntity> nodes,
        IDictionary<string, LayoutPoint> coordinatesByNodeKey,
        double centerX,
        double step,
        double y,
        double minX,
        double maxX)
    {
        if (nodes.Count == 0)
        {
            return;
        }

        var rowStartX = centerX - (((nodes.Count - 1) * step) / 2d);
        for (var index = 0; index < nodes.Count; index++)
        {
            var node = nodes[index];
            if (coordinatesByNodeKey.ContainsKey(node.NodeKey))
            {
                continue;
            }

            var x = Math.Clamp(rowStartX + (index * step), minX, maxX);
            coordinatesByNodeKey[node.NodeKey] = new LayoutPoint(x, Math.Clamp(y, MinY, MaxY));
        }
    }

    private static bool HasAreaChildren(
        string nodeKey,
        string areaKey,
        IReadOnlyDictionary<string, List<SchematicNodeEntity>> childrenByParentKey,
        IReadOnlyDictionary<string, string> areaByNodeKey)
    {
        if (!childrenByParentKey.TryGetValue(nodeKey, out var children) || children.Count == 0)
        {
            return false;
        }

        return children.Any(child => string.Equals(areaByNodeKey.GetValueOrDefault(child.NodeKey), areaKey, StringComparison.OrdinalIgnoreCase));
    }

    private static int GetAreaSortRank(string areaKey)
    {
        var preferredIndex = Array.FindIndex(PreferredAreaOrder, area => area.Equals(areaKey, StringComparison.OrdinalIgnoreCase));
        if (preferredIndex >= 0)
        {
            return preferredIndex;
        }

        return PreferredAreaOrder.Length + 1;
    }

    private static Dictionary<string, string> BuildAreaLookup(
        IEnumerable<SchematicNodeEntity> nodes,
        IReadOnlyDictionary<string, string?> parentByNodeKey,
        IReadOnlyDictionary<string, List<SchematicNodeEntity>> childrenByParentKey)
    {
        var nodeList = nodes.ToList();
        var areaByNodeKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in nodeList)
        {
            if (IsGenerationNode(node) || IsUtilityNode(node))
            {
                areaByNodeKey[node.NodeKey] = "utility_branches";
                continue;
            }

            var directArea = ResolveAreaKey(node.Zone) ?? ResolveAreaKey($"{node.NodeKey} {node.Label}");
            if (!string.IsNullOrWhiteSpace(directArea))
            {
                areaByNodeKey[node.NodeKey] = directArea;
            }
        }

        var anchors = nodeList
            .Where(IsAreaAnchorNode)
            .OrderBy(node => node.NodeKey, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var anchor in anchors)
        {
            if (!areaByNodeKey.TryGetValue(anchor.NodeKey, out var areaKey) || string.IsNullOrWhiteSpace(areaKey))
            {
                continue;
            }

            var stack = new Stack<string>();
            stack.Push(anchor.NodeKey);

            while (stack.Count > 0)
            {
                var currentNodeKey = stack.Pop();
                areaByNodeKey[currentNodeKey] = areaKey;

                if (!childrenByParentKey.TryGetValue(currentNodeKey, out var children) || children.Count == 0)
                {
                    continue;
                }

                foreach (var child in children)
                {
                    if (IsGenerationNode(child) || IsUtilityNode(child) || IsGridBackboneNode(child))
                    {
                        continue;
                    }

                    if (areaByNodeKey.TryGetValue(child.NodeKey, out var existingArea)
                        && !string.IsNullOrWhiteSpace(existingArea)
                        && !string.Equals(existingArea, areaKey, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    stack.Push(child.NodeKey);
                }
            }
        }

        var depthByNodeKey = BuildDepthLookup(nodeList, parentByNodeKey, childrenByParentKey);
        foreach (var node in nodeList.OrderBy(node => depthByNodeKey.GetValueOrDefault(node.NodeKey, 0)))
        {
            if (areaByNodeKey.ContainsKey(node.NodeKey))
            {
                continue;
            }

            if (parentByNodeKey.TryGetValue(node.NodeKey, out var parentKey)
                && !string.IsNullOrWhiteSpace(parentKey)
                && areaByNodeKey.TryGetValue(parentKey, out var parentArea)
                && !string.Equals(parentArea, "backbone", StringComparison.OrdinalIgnoreCase))
            {
                areaByNodeKey[node.NodeKey] = parentArea;
                continue;
            }

            if (IsGridBackboneNode(node))
            {
                areaByNodeKey[node.NodeKey] = "backbone";
                continue;
            }

            areaByNodeKey[node.NodeKey] = "backbone";
        }

        return areaByNodeKey;
    }

    private static Dictionary<string, int> BuildDepthLookup(
        IReadOnlyList<SchematicNodeEntity> nodes,
        IReadOnlyDictionary<string, string?> parentByNodeKey,
        IReadOnlyDictionary<string, List<SchematicNodeEntity>> childrenByParentKey)
    {
        var depthByNodeKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var nodeByKey = nodes.ToDictionary(node => node.NodeKey, StringComparer.OrdinalIgnoreCase);
        var roots = nodes
            .Where(node => !parentByNodeKey.TryGetValue(node.NodeKey, out var parentKey)
                || string.IsNullOrWhiteSpace(parentKey)
                || !nodeByKey.ContainsKey(parentKey))
            .ToList();

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

            if (!childrenByParentKey.TryGetValue(nodeKey, out var children) || children.Count == 0)
            {
                continue;
            }

            foreach (var child in children)
            {
                queue.Enqueue((child.NodeKey, depth + 1));
            }
        }

        var maxDepth = depthByNodeKey.Values.DefaultIfEmpty(0).Max();
        foreach (var node in nodes)
        {
            if (!depthByNodeKey.ContainsKey(node.NodeKey))
            {
                maxDepth++;
                depthByNodeKey[node.NodeKey] = maxDepth;
            }
        }

        return depthByNodeKey;
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
            if (!string.IsNullOrWhiteSpace(parentKey)
                && nodeKeys.Contains(parentKey)
                && !parentKey.Equals(node.NodeKey, StringComparison.OrdinalIgnoreCase))
            {
                parentByNodeKey[node.NodeKey] = parentKey;
                continue;
            }

            if (fallbackParentByNodeKey.TryGetValue(node.NodeKey, out var fallbackParent)
                && !fallbackParent.Equals(node.NodeKey, StringComparison.OrdinalIgnoreCase))
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
            if (string.IsNullOrWhiteSpace(parentKey) || !nodeByKey.TryGetValue(nodeKey, out var child))
            {
                continue;
            }

            if (!childrenByParentKey.TryGetValue(parentKey, out var children))
            {
                children = [];
                childrenByParentKey[parentKey] = children;
            }

            children.Add(child);
        }

        foreach (var children in childrenByParentKey.Values)
        {
            children.Sort((left, right) =>
            {
                var rankCompare = GetNodeSortRank(left).CompareTo(GetNodeSortRank(right));
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
            });
        }

        return childrenByParentKey;
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
                    .OrderBy(sourceNodeKey => sourceNodeKey, StringComparer.OrdinalIgnoreCase)
                    .First(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsGridBackboneNode(SchematicNodeEntity node)
    {
        var type = node.NodeType?.Trim();
        if (string.IsNullOrWhiteSpace(type))
        {
            return false;
        }

        return type.Equals("grid", StringComparison.OrdinalIgnoreCase)
            || type.Equals("site", StringComparison.OrdinalIgnoreCase)
            || type.Equals("building", StringComparison.OrdinalIgnoreCase)
            || type.Equals("transformer", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGenerationNode(SchematicNodeEntity node)
    {
        var type = node.NodeType?.Trim();
        return type is not null
            && (type.Equals("generator_pv", StringComparison.OrdinalIgnoreCase)
                || type.Equals("generator_chp", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsUtilityNode(SchematicNodeEntity node)
    {
        var type = node.NodeType?.Trim();
        return type is not null
            && (type.Equals("utility_cooling", StringComparison.OrdinalIgnoreCase)
                || type.Equals("utility_heating", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsAreaAnchorNode(SchematicNodeEntity node)
    {
        return string.Equals(node.NodeType, "zone", StringComparison.OrdinalIgnoreCase);
    }

    private static int GetNodeSortRank(SchematicNodeEntity node)
    {
        if (node.NodeKey.Equals("ext_grid", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var type = node.NodeType?.Trim().ToLowerInvariant();
        return type switch
        {
            "grid" or "site" or "building" => 1,
            "transformer" => 2,
            "generator_chp" => 3,
            "generator_pv" => 4,
            "utility_cooling" => 5,
            "utility_heating" => 6,
            "zone" => 7,
            "subzone" => 8,
            _ => 10
        };
    }

    private static int GetTransformerSortRank(SchematicNodeEntity node)
    {
        if (node.NodeKey.Contains("vz81", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (node.NodeKey.Contains("vz82", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (node.NodeKey.Contains("h2z35", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (node.NodeKey.Contains("h2z36", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        return 4;
    }

    private static int GetGenerationUtilitySortRank(SchematicNodeEntity node)
    {
        if (node.NodeKey.Contains("cool", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (node.NodeKey.Contains("heat", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (node.NodeKey.Contains("chp", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (node.NodeKey.Contains("pv", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        return 4;
    }

    private static string? ResolveAreaKey(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var normalized = raw.Trim().ToLowerInvariant();

        if (normalized.Contains("emission", StringComparison.OrdinalIgnoreCase) || normalized.Contains("em_", StringComparison.OrdinalIgnoreCase))
        {
            return "emission_lab";
        }

        if (normalized.Contains("design", StringComparison.OrdinalIgnoreCase) || normalized.Contains("ds_", StringComparison.OrdinalIgnoreCase))
        {
            return "design_studio";
        }

        if (normalized.Contains("workshop", StringComparison.OrdinalIgnoreCase) || normalized.Contains("robotlab", StringComparison.OrdinalIgnoreCase))
        {
            return "workshop";
        }

        if (normalized.Contains("office", StringComparison.OrdinalIgnoreCase))
        {
            return "offices";
        }

        return null;
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
            TargetNodeKey = source.TargetNodeKey,
            RelationshipKind = source.RelationshipKind,
            IsLayoutEdge = source.IsLayoutEdge,
            Note = source.Note
        };
    }

    private sealed record LayoutPoint(double X, double Y);

    private sealed class AreaNodeGroups(
        List<SchematicNodeEntity> anchorNodes,
        List<SchematicNodeEntity> distributionNodes,
        List<SchematicNodeEntity> endpointNodes)
    {
        public List<SchematicNodeEntity> AnchorNodes { get; } = anchorNodes;
        public List<SchematicNodeEntity> DistributionNodes { get; } = distributionNodes;
        public List<SchematicNodeEntity> EndpointNodes { get; } = endpointNodes;
    }
}
