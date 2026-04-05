using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quay27.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SheetPickerDraftStaffNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SheetPickerDraftStaffNames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DisplayName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SheetPickerDraftStaffNames", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_SheetPickerDraftStaffNames_SortOrder",
                table: "SheetPickerDraftStaffNames",
                column: "SortOrder");

            // Preserve prior NV soạn picks: member user rows -> display names (MySQL 8+).
            migrationBuilder.Sql(
                """
                INSERT INTO `SheetPickerDraftStaffNames` (`DisplayName`, `SortOrder`)
                SELECT `FullName`, `rn` - 1 FROM (
                    SELECT u.`FullName`, ROW_NUMBER() OVER (ORDER BY u.`FullName`) AS `rn`
                    FROM `SheetPickerMembers` sp
                    INNER JOIN `Users` u ON u.`Id` = sp.`UserId`
                    WHERE u.`IsActive` = 1
                ) AS `t`
                """);

            migrationBuilder.DropTable(
                name: "SheetPickerMembers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SheetPickerDraftStaffNames");

            migrationBuilder.CreateTable(
                name: "SheetPickerMembers",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SheetPickerMembers", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_SheetPickerMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
