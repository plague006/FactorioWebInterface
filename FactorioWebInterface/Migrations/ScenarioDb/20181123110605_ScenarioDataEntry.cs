using Microsoft.EntityFrameworkCore.Migrations;

namespace FactorioWebInterface.Migrations.ScenarioDb
{
    public partial class ScenarioDataEntry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScenarioDataEntries",
                columns: table => new
                {
                    DataSet = table.Column<string>(nullable: false),
                    Key = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioDataEntries", x => new { x.DataSet, x.Key });
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioDataEntries_DataSet",
                table: "ScenarioDataEntries",
                column: "DataSet");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScenarioDataEntries");
        }
    }
}
