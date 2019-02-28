using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FactorioWebInterface.Migrations
{
    public partial class NamedDiscordServers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Donators");

            migrationBuilder.DropTable(
                name: "Regulars");

            migrationBuilder.CreateTable(
                name: "NamedDiscordServers",
                columns: table => new
                {
                    DiscordChannelId = table.Column<ulong>(nullable: false),
                    Name = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NamedDiscordServers", x => new { x.DiscordChannelId, x.Name });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NamedDiscordServers");

            migrationBuilder.CreateTable(
                name: "Donators",
                columns: table => new
                {
                    Name = table.Column<string>(nullable: false),
                    Perks = table.Column<int>(nullable: false),
                    WelcomeMessage = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Donators", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "Regulars",
                columns: table => new
                {
                    Name = table.Column<string>(nullable: false),
                    Date = table.Column<DateTimeOffset>(nullable: false),
                    PromotedBy = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regulars", x => x.Name);
                });
        }
    }
}
