using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaktyxAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiSelectFieldType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MultiSelectValue",
                table: "SkillFieldValues",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MultiSelectValue",
                table: "SkillFieldValues");
        }
    }
}
