using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobManagement.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class schemaChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Job",
                schema: "Dagable",
                newName: "Job");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Dagable");

            migrationBuilder.RenameTable(
                name: "Job",
                newName: "Job",
                newSchema: "Dagable");
        }
    }
}
