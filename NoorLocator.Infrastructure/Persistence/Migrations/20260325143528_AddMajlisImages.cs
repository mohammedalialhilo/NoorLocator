using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoorLocator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMajlisImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Majalis",
                type: "varchar(512)",
                maxLength: 512,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Majalis");
        }
    }
}
