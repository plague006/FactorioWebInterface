using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FactorioWebInterface.Migrations
{
    public partial class Bans : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bans",
                columns: table => new
                {
                    Username = table.Column<string>(nullable: false),
                    Reason = table.Column<string>(nullable: true),
                    Address = table.Column<string>(nullable: true),
                    Admin = table.Column<string>(nullable: true),
                    DateTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bans", x => x.Username);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bans");
        }
    }
}
