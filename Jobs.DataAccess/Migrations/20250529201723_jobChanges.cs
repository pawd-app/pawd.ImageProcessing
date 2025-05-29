using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobManagement.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class jobChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RequestGuid",
                schema: "Dagable",
                table: "Job",
                newName: "JobGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "JobGuid",
                schema: "Dagable",
                table: "Job",
                newName: "RequestGuid");
        }
    }
}
