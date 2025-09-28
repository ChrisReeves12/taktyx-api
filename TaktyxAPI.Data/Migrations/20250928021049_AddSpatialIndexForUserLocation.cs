using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaktyxAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSpatialIndexForUserLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE: This spatial index requires CLR to be enabled on SQL Server.
            // It will fail on SQL Server Express but works on Standard/Enterprise/Azure SQL.
            // To enable CLR in production: EXEC sp_configure 'clr enabled', 1; RECONFIGURE;
            migrationBuilder.Sql(@"
                CREATE SPATIAL INDEX IX_Users_Location
                ON Users(Location)
                USING GEOGRAPHY_GRID
                WITH (
                    GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM),
                    CELLS_PER_OBJECT = 16
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IX_Users_Location ON Users;");
        }
    }
}
