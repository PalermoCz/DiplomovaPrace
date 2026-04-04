using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DiplomovaPrace.Persistence;
using DiplomovaPrace.Persistence.Schematic;
using Microsoft.EntityFrameworkCore;

namespace DiplomovaPrace.Services;

/// <summary>
/// Import služba pro facility-centric schematic model (Sprint 2).
/// Načte mvp_nodes.csv a mvp_edges.csv a uloží facility, nodes, edges
/// a základní measurement mapy do SQLite.
///
/// Idempotentní: pokud facility "Smart Company Facility" již existuje, seed se přeskočí.
///
/// Hledá CSV soubory v tomto pořadí:
///   1. DataSet/Facility/ (vedle složky DiplomovaPrace projektu)
///   2. ContentRoot projektu (záložní umístění)
/// </summary>
public class FacilityImportService
{
    private const string FacilityName = "Smart Company Facility";

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<FacilityImportService> _logger;

    public FacilityImportService(
        IDbContextFactory<AppDbContext> dbFactory,
        ILogger<FacilityImportService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>
    /// Spustí seed facility modelu z CSV souborů.
    /// Pokud facility již existuje, operace se přeskočí.
    /// </summary>
    public async Task SeedAsync(string contentRootPath, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // ── Idempotence check: pokud facility existuje, přeskočíme seed ───────
        var alreadyExists = await db.Facilities
            .AnyAsync(f => f.Name == FacilityName, ct);

        if (alreadyExists)
        {
            _logger.LogInformation("Facility '{Name}' již existuje. Seed se přeskočí.", FacilityName);
            return;
        }

        // ── Lokalizace CSV souborů ────────────────────────────────────────────
        var (nodesPath, edgesPath) = ResolveCsvPaths(contentRootPath);

        if (!File.Exists(nodesPath))
        {
            _logger.LogWarning("Soubor mvp_nodes.csv nebyl nalezen na cestě: {Path}. Seed se přeskočí.", nodesPath);
            return;
        }

        if (!File.Exists(edgesPath))
        {
            _logger.LogWarning("Soubor mvp_edges.csv nebyl nalezen na cestě: {Path}. Seed se přeskočí.", edgesPath);
            return;
        }

        _logger.LogInformation("Zahajuji seed facility '{Name}'...", FacilityName);

        // ── 1. Vytvoření facility ─────────────────────────────────────────────
        var facility = new FacilityEntity
        {
            Name = FacilityName,
            Description = "Hlavní provozovna — schematický model v MVP konfiguraci.",
            TimeZone = "Europe/Prague"
        };
        db.Facilities.Add(facility);
        await db.SaveChangesAsync(ct); // Uložení pro získání Id

        // ── 2. Načtení a uložení nodes ────────────────────────────────────────
        var nodeRows = ReadNodesCsv(nodesPath);
        var nodeEntities = nodeRows.Select(row => new SchematicNodeEntity
        {
            FacilityId   = facility.Id,
            NodeKey      = row.NodeId,
            Label        = row.Label,
            NodeType     = NullIfEmpty(row.NodeType),
            Zone         = NullIfEmpty(row.Zone),
            MeterUrn     = NullIfEmpty(row.MeterUrn),
            ParentNodeKey = NullIfEmpty(row.ParentNodeId),
            XHint        = ParseDoubleOrNull(row.XHint),
            YHint        = ParseDoubleOrNull(row.YHint),
        }).ToList();

        db.SchematicNodes.AddRange(nodeEntities);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Uloženo {Count} uzlů (nodes).", nodeEntities.Count);

        // ── 3. Načtení explicitních hran z mvp_edges.csv ──────────────────────
        var explicitEdges = ReadEdgesCsv(edgesPath);

        // Pracujeme s HashSetem (source, target) pro detekci duplicit
        var edgeSet = new HashSet<(string Source, string Target)>(
            explicitEdges.Select(e => (e.SourceNodeId, e.TargetNodeId))
        );

        // ── 4. Dopočítání chybějících hran z parent_node_id ───────────────────
        // Pokud node má ParentNodeKey a hrana ParentNodeKey -> NodeKey ještě neexistuje,
        // přidáme ji automaticky — zachová kompletní interní hierarchii grafu.
        foreach (var node in nodeEntities)
        {
            if (string.IsNullOrEmpty(node.ParentNodeKey)) continue;

            var derivedEdge = (Source: node.ParentNodeKey, Target: node.NodeKey);
            if (edgeSet.Add(derivedEdge))
            {
                _logger.LogDebug("Dopočítána hrana z parent_node_id: {Source} -> {Target}",
                    derivedEdge.Source, derivedEdge.Target);
            }
        }

        // ── 5. Uložení finálního edge listu ───────────────────────────────────
        var edgeEntities = edgeSet.Select(e => new SchematicEdgeEntity
        {
            FacilityId    = facility.Id,
            SourceNodeKey = e.Source,
            TargetNodeKey = e.Target,
        }).ToList();

        db.SchematicEdges.AddRange(edgeEntities);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Uloženo {Count} hran (edges) — {Explicit} explicitních + {Derived} dopočítaných.",
            edgeEntities.Count,
            explicitEdges.Count,
            edgeEntities.Count - explicitEdges.Count);

        // ── 6. Seed FacilityMeasurementMap pro nodes s MeterUrn ──────────────
        var measurementMaps = nodeEntities
            .Where(n => !string.IsNullOrEmpty(n.MeterUrn))
            .Select(n => new FacilityMeasurementMapEntity
            {
                FacilityId      = facility.Id,
                NodeKey         = n.NodeKey,
                MeterUrn        = n.MeterUrn!,
                MeasurementKind = "electricity",
            }).ToList();

        if (measurementMaps.Count > 0)
        {
            db.FacilityMeasurementMaps.AddRange(measurementMaps);
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Uloženo {Count} záznamů FacilityMeasurementMap.", measurementMaps.Count);
        }

        _logger.LogInformation("Seed facility '{Name}' dokončen. Uzly: {Nodes}, Hrany: {Edges}.",
            FacilityName, nodeEntities.Count, edgeEntities.Count);
    }

    // ── CSV parsování ─────────────────────────────────────────────────────────

    private static List<NodeCsvRow> ReadNodesCsv(string path)
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,       // toleruje chybějící volitelné sloupce
        });
        return csv.GetRecords<NodeCsvRow>().ToList();
    }

    private static List<EdgeCsvRow> ReadEdgesCsv(string path)
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
        });
        return csv.GetRecords<EdgeCsvRow>().ToList();
    }

    // ── Lokalizace CSV souborů ────────────────────────────────────────────────

    private static (string NodesPath, string EdgesPath) ResolveCsvPaths(string contentRootPath)
    {
        // Preferovaná cesta: DataSet/Facility/ (vedle složky projektu)
        var datasetFolder = Path.Combine(contentRootPath, "..", "DataSet", "Facility");
        var preferredNodes = Path.GetFullPath(Path.Combine(datasetFolder, "mvp_nodes.csv"));
        var preferredEdges = Path.GetFullPath(Path.Combine(datasetFolder, "mvp_edges.csv"));

        if (File.Exists(preferredNodes) && File.Exists(preferredEdges))
            return (preferredNodes, preferredEdges);

        // Záložní: ContentRoot projektu (pro starší umístění během vývoje)
        var fallbackNodes = Path.Combine(contentRootPath, "mvp_nodes.csv");
        var fallbackEdges = Path.Combine(contentRootPath, "mvp_edges.csv");

        return (
            File.Exists(fallbackNodes) ? fallbackNodes : preferredNodes,
            File.Exists(fallbackEdges) ? fallbackEdges : preferredEdges
        );
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static double? ParseDoubleOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return double.TryParse(value.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
            ? d : null;
    }

    // ── Interní CSV record modely ─────────────────────────────────────────────

    private sealed class NodeCsvRow
    {
        [CsvHelper.Configuration.Attributes.Name("node_id")]
        public string NodeId { get; set; } = string.Empty;

        [CsvHelper.Configuration.Attributes.Name("label")]
        public string Label { get; set; } = string.Empty;

        [CsvHelper.Configuration.Attributes.Name("node_type")]
        public string NodeType { get; set; } = string.Empty;

        [CsvHelper.Configuration.Attributes.Name("zone")]
        public string Zone { get; set; } = string.Empty;

        [CsvHelper.Configuration.Attributes.Name("meter_urn")]
        public string MeterUrn { get; set; } = string.Empty;

        [CsvHelper.Configuration.Attributes.Name("parent_node_id")]
        public string ParentNodeId { get; set; } = string.Empty;

        [CsvHelper.Configuration.Attributes.Name("x_hint")]
        public string XHint { get; set; } = string.Empty;

        [CsvHelper.Configuration.Attributes.Name("y_hint")]
        public string YHint { get; set; } = string.Empty;
    }

    private sealed class EdgeCsvRow
    {
        [CsvHelper.Configuration.Attributes.Name("source_node_id")]
        public string SourceNodeId { get; set; } = string.Empty;

        [CsvHelper.Configuration.Attributes.Name("target_node_id")]
        public string TargetNodeId { get; set; } = string.Empty;
    }
}
