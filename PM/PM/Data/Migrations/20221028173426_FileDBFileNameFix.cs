using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PM.Data.Migrations
{
    public partial class FileDBFileNameFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "FileDB",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                table: "FileDB");
        }
    }
}
