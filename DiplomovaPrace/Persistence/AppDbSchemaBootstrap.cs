using System.Data.Common;
using DiplomovaPrace.Persistence.Schematic;
using Microsoft.EntityFrameworkCore;

namespace DiplomovaPrace.Persistence;

public static class AppDbSchemaBootstrap
{
    public static async Task EnsureFacilityMembershipSchemaAsync(AppDbContext db, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);

        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""FacilityMemberships"" (
                ""Id"" INTEGER PRIMARY KEY AUTOINCREMENT,
                ""FacilityId"" INTEGER NOT NULL,
                ""AppUserId"" INTEGER NOT NULL,
                ""Role"" TEXT NOT NULL,
                ""CreatedAtUtc"" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY(""FacilityId"") REFERENCES ""Facilities""(""Id"") ON DELETE CASCADE,
                FOREIGN KEY(""AppUserId"") REFERENCES ""AppUsers""(""Id"") ON DELETE CASCADE
            );
        ");

        await db.Database.ExecuteSqlRawAsync(@"
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_FacilityMemberships_FacilityId_AppUserId""
            ON ""FacilityMemberships"" (""FacilityId"", ""AppUserId"");
        ");

        await db.Database.ExecuteSqlRawAsync(@"
            CREATE INDEX IF NOT EXISTS ""IX_FacilityMemberships_AppUserId""
            ON ""FacilityMemberships"" (""AppUserId"");
        ");
    }

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
        const string relationshipKindExpression = @"
CASE
    WHEN EXISTS (
        SELECT 1
        FROM ""SchematicNodes"" AS n
        WHERE n.""FacilityId"" = se.""FacilityId""
          AND lower(n.""NodeKey"") = lower(se.""TargetNodeKey"")
          AND n.""ParentNodeKey"" IS NOT NULL
          AND lower(n.""ParentNodeKey"") = lower(se.""SourceNodeKey"")
    ) THEN 'layout_primary'
    WHEN trim(coalesce(se.""RelationshipKind"", '')) = '' THEN 'semantic'
    ELSE lower(trim(se.""RelationshipKind""))
END";

        const string isLayoutEdgeExpression = @"
CASE
    WHEN EXISTS (
        SELECT 1
        FROM ""SchematicNodes"" AS n
        WHERE n.""FacilityId"" = se.""FacilityId""
          AND lower(n.""NodeKey"") = lower(se.""TargetNodeKey"")
          AND n.""ParentNodeKey"" IS NOT NULL
          AND lower(n.""ParentNodeKey"") = lower(se.""SourceNodeKey"")
    ) THEN 1
    WHEN coalesce(se.""IsLayoutEdge"", 0) <> 0 THEN 1
    ELSE 0
END";

        var deduplicateSql = $@"
WITH normalized AS (
    SELECT
        se.""Id"",
        se.""FacilityId"",
        se.""SourceNodeKey"",
        se.""TargetNodeKey"",
        {relationshipKindExpression} AS normalized_relationship_kind,
        {isLayoutEdgeExpression} AS normalized_is_layout_edge,
        trim(coalesce(se.""Note"", '')) AS normalized_note
    FROM ""SchematicEdges"" AS se
), ranked AS (
    SELECT
        n.""Id"",
        row_number() OVER (
            PARTITION BY n.""FacilityId"", n.""SourceNodeKey"", n.""TargetNodeKey"", n.normalized_relationship_kind
            ORDER BY
                CASE WHEN n.normalized_is_layout_edge <> 0 THEN 0 ELSE 1 END,
                CASE WHEN n.normalized_note <> '' THEN 0 ELSE 1 END,
                n.""Id""
        ) AS rn
    FROM normalized AS n
)
DELETE FROM ""SchematicEdges""
WHERE ""Id"" IN (
    SELECT ""Id""
    FROM ranked
    WHERE rn > 1
);";

        var backfillSql = $@"
UPDATE ""SchematicEdges"" AS se
SET
    ""RelationshipKind"" = {relationshipKindExpression},
    ""IsLayoutEdge"" = {isLayoutEdgeExpression};";

        await db.Database.ExecuteSqlRawAsync(deduplicateSql, cancellationToken: ct);
        await db.Database.ExecuteSqlRawAsync(backfillSql, cancellationToken: ct);
    }
}