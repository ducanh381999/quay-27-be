using Microsoft.EntityFrameworkCore.Migrations;

namespace Quay27.Infrastructure.Persistence.Migrations;

/// <summary>
/// Idempotent schema helpers for MySQL when DB may already contain objects from a partial/failed migration.
/// </summary>
internal static class MigrationSqlHelper
{
    /// <summary>Matches Pomelo Guid FK columns (see PurchaseOrders ReceivingAccountId migration).</summary>
    public const string GuidFkColumnNullable =
        "char(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL";

    public const string Varchar32Nullable =
        "varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL";

    /// <summary>Add Guid FK column if missing, or MODIFY to ascii collation when already present.</summary>
    public static void EnsureGuidFkColumn(
        MigrationBuilder migrationBuilder,
        string table,
        string column)
    {
        AddColumnIfNotExists(migrationBuilder, table, column, GuidFkColumnNullable);

        migrationBuilder.Sql($"""
            SET @__q27_exists := (
                SELECT COUNT(*)
                FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = '{table}'
                  AND COLUMN_NAME = '{column}'
            );
            SET @__q27_sql := IF(
                @__q27_exists > 0,
                'ALTER TABLE `{table}` MODIFY COLUMN `{column}` {GuidFkColumnNullable}',
                'SELECT 1'
            );
            PREPARE __q27_stmt FROM @__q27_sql;
            EXECUTE __q27_stmt;
            DEALLOCATE PREPARE __q27_stmt;
            """);
    }

    public static void AddColumnIfNotExists(
        MigrationBuilder migrationBuilder,
        string table,
        string column,
        string columnDefinitionSql)
    {
        migrationBuilder.Sql($"""
            SET @__q27_exists := (
                SELECT COUNT(*)
                FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = '{table}'
                  AND COLUMN_NAME = '{column}'
            );
            SET @__q27_sql := IF(
                @__q27_exists = 0,
                'ALTER TABLE `{table}` ADD `{column}` {columnDefinitionSql}',
                'SELECT 1'
            );
            PREPARE __q27_stmt FROM @__q27_sql;
            EXECUTE __q27_stmt;
            DEALLOCATE PREPARE __q27_stmt;
            """);
    }

    public static void CreateIndexIfNotExists(
        MigrationBuilder migrationBuilder,
        string indexName,
        string table,
        string column)
    {
        migrationBuilder.Sql($"""
            SET @__q27_exists := (
                SELECT COUNT(*)
                FROM information_schema.STATISTICS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = '{table}'
                  AND INDEX_NAME = '{indexName}'
            );
            SET @__q27_sql := IF(
                @__q27_exists = 0,
                'CREATE INDEX `{indexName}` ON `{table}` (`{column}`)',
                'SELECT 1'
            );
            PREPARE __q27_stmt FROM @__q27_sql;
            EXECUTE __q27_stmt;
            DEALLOCATE PREPARE __q27_stmt;
            """);
    }

    public static void AddForeignKeyIfNotExists(
        MigrationBuilder migrationBuilder,
        string foreignKeyName,
        string table,
        string column,
        string principalTable,
        string principalColumn,
        string onDeleteSql)
    {
        migrationBuilder.Sql($"""
            SET @__q27_exists := (
                SELECT COUNT(*)
                FROM information_schema.TABLE_CONSTRAINTS
                WHERE CONSTRAINT_SCHEMA = DATABASE()
                  AND TABLE_NAME = '{table}'
                  AND CONSTRAINT_NAME = '{foreignKeyName}'
                  AND CONSTRAINT_TYPE = 'FOREIGN KEY'
            );
            SET @__q27_sql := IF(
                @__q27_exists = 0,
                'ALTER TABLE `{table}` ADD CONSTRAINT `{foreignKeyName}` FOREIGN KEY (`{column}`) REFERENCES `{principalTable}` (`{principalColumn}`) ON DELETE {onDeleteSql}',
                'SELECT 1'
            );
            PREPARE __q27_stmt FROM @__q27_sql;
            EXECUTE __q27_stmt;
            DEALLOCATE PREPARE __q27_stmt;
            """);
    }

    public static void DropForeignKeyIfExists(
        MigrationBuilder migrationBuilder,
        string foreignKeyName,
        string table)
    {
        migrationBuilder.Sql($"""
            SET @__q27_exists := (
                SELECT COUNT(*)
                FROM information_schema.TABLE_CONSTRAINTS
                WHERE CONSTRAINT_SCHEMA = DATABASE()
                  AND TABLE_NAME = '{table}'
                  AND CONSTRAINT_NAME = '{foreignKeyName}'
                  AND CONSTRAINT_TYPE = 'FOREIGN KEY'
            );
            SET @__q27_sql := IF(
                @__q27_exists > 0,
                'ALTER TABLE `{table}` DROP FOREIGN KEY `{foreignKeyName}`',
                'SELECT 1'
            );
            PREPARE __q27_stmt FROM @__q27_sql;
            EXECUTE __q27_stmt;
            DEALLOCATE PREPARE __q27_stmt;
            """);
    }

    public static void DropIndexIfExists(
        MigrationBuilder migrationBuilder,
        string indexName,
        string table)
    {
        migrationBuilder.Sql($"""
            SET @__q27_exists := (
                SELECT COUNT(*)
                FROM information_schema.STATISTICS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = '{table}'
                  AND INDEX_NAME = '{indexName}'
            );
            SET @__q27_sql := IF(
                @__q27_exists > 0,
                'DROP INDEX `{indexName}` ON `{table}`',
                'SELECT 1'
            );
            PREPARE __q27_stmt FROM @__q27_sql;
            EXECUTE __q27_stmt;
            DEALLOCATE PREPARE __q27_stmt;
            """);
    }

    public static void DropColumnIfExists(
        MigrationBuilder migrationBuilder,
        string table,
        string column)
    {
        migrationBuilder.Sql($"""
            SET @__q27_exists := (
                SELECT COUNT(*)
                FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = '{table}'
                  AND COLUMN_NAME = '{column}'
            );
            SET @__q27_sql := IF(
                @__q27_exists > 0,
                'ALTER TABLE `{table}` DROP COLUMN `{column}`',
                'SELECT 1'
            );
            PREPARE __q27_stmt FROM @__q27_sql;
            EXECUTE __q27_stmt;
            DEALLOCATE PREPARE __q27_stmt;
            """);
    }
}
