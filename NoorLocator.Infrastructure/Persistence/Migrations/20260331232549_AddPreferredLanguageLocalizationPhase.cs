using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorLocator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredLanguageLocalizationPhase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredLanguageCode",
                table: "Users",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredLanguageCode",
                table: "Users");
        }
    }
}
