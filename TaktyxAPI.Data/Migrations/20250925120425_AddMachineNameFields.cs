using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaktyxAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMachineNameFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MachineName",
                table: "Skills",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MachineName",
                table: "SkillFields",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_MachineName",
                table: "Skills",
                column: "MachineName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillFields_MachineName",
                table: "SkillFields",
                column: "MachineName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Skills_MachineName",
                table: "Skills");

            migrationBuilder.DropIndex(
                name: "IX_SkillFields_MachineName",
                table: "SkillFields");

            migrationBuilder.DropColumn(
                name: "MachineName",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "MachineName",
                table: "SkillFields");
        }
    }
}
