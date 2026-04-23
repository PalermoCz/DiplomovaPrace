using System.Data.Common;
using DiplomovaPrace.Persistence.Schematic;
using Microsoft.EntityFrameworkCore;

namespace DiplomovaPrace.Persistence;

public static class AppDbSchemaBootstrap
{
    public static async Task EnsurePhaseOneRelationshipSchemaAsync(AppDbContext db, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);

        await db.Database.OpenConnectionAsync(ct);

        try
        {
            var columns = await GetColumnNamesAsync(db, "SchematicEdges", ct);

            if (!columns.Contains("RelationshipKind", StringComparer.OrdinalIgnoreCase))
            {
                await db.Database.ExecuteSqlRawAsync($"ALTER TABLE \"SchematicEdges\" ADD COLUMN \"RelationshipKind\" TEXT NOT NULL DEFAULT '{SchematicRelationshipKinds.Semantic}'");
            }

            if (!columns.Contains("IsLayoutEdge", StringComparer.OrdinalIgnoreCase))
            {
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"SchematicEdges\" ADD COLUMN \"IsLayoutEdge\" INTEGER NOT NULL DEFAULT 0");
            }

            if (!columns.Contains("Note", StringComparer.OrdinalIgnoreCase))
            {
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"SchematicEdges\" ADD COLUMN \"Note\" TEXT NULL");
            }

            await BackfillRelationshipMetadataAsync(db, ct);
            await EnsureRelationshipIndexAsync(db, ct);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static async Task<HashSet<string>> GetColumnNamesAsync(AppDbContext db, string tableName, CancellationToken ct)
    {
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var reader = await command.ExecuteReaderAsync(ct);
        var nameOrdinal = reader.GetOrdinal("name");
        while (await reader.ReadAsync(ct))
        {
            columns.Add(reader.GetString(nameOrdinal));
        }

        return columns;
    }

    private static async Task EnsureRelationshipIndexAsync(AppDbContext db, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("DROP INDEX IF EXISTS \"IX_SchematicEdges_FacilityId_Source_Target\"");
        await db.Database.ExecuteSqlRawAsync("DROP INDEX IF EXISTS \"IX_SchematicEdges_FacilityId_Source_Target_RelationshipKind\"");
        await db.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_SchematicEdges_FacilityId_Source_Target_RelationshipKind\" ON \"SchematicEdges\" (\"FacilityId\", \"SourceNodeKey\", \"TargetNodeKey\", \"RelationshipKind\")");
    }

    private static async Task BackfillRelationshipMetadataAsync(AppDbContext db, CancellationToken ct)
    {
        const string sql = @"
UPDATE ""SchematicEdges""
SET
    ""RelationshipKind"" = CASE
        WHEN EXISTS (
            SELECT 1
            FROM ""SchematicNodes"" AS n
            WHERE n.""FacilityId"" = ""SchematicEdges"".""FacilityId""
              AND lower(n.""NodeKey"") = lower(""SchematicEdges"".""TargetNodeKey"")
              AND n.""ParentNodeKey"" IS NOT NULL
              AND lower(n.""ParentNodeKey"") = lower(""SchematicEdges"".""SourceNodeKey"")
        ) THEN 'layout_primary'
        WHEN trim(coalesce(""RelationshipKind"", '')) = '' THEN 'semantic'
        ELSE lower(trim(""RelationshipKind""))
    END,
    ""IsLayoutEdge"" = CASE
        WHEN EXISTS (
            SELECT 1
            FROM ""SchematicNodes"" AS n
            WHERE n.""FacilityId"" = ""SchematicEdges"".""FacilityId""
              AND lower(n.""NodeKey"") = lower(""SchematicEdges"".""TargetNodeKey"")
              AND n.""ParentNodeKey"" IS NOT NULL
              AND lower(n.""ParentNodeKey"") = lower(""SchematicEdges"".""SourceNodeKey"")
        ) THEN 1
        WHEN coalesce(""IsLayoutEdge"", 0) <> 0 THEN 1
        ELSE 0
    END;";

        await db.Database.ExecuteSqlRawAsync(sql, cancellationToken: ct);
    }
}