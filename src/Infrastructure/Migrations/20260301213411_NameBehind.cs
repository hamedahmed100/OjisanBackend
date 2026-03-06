using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OjisanBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NameBehind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NameBehind",
                table: "Groups",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameBehind",
                table: "Groups");
        }
    }
}
