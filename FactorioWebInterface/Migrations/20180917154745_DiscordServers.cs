using Microsoft.EntityFrameworkCore.Migrations;

namespace FactorioWebInterface.Migrations
{
    public partial class DiscordServers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscordServers",
                columns: table => new
                {
                    DiscordChannelId = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServerId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordServers", x => x.DiscordChannelId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscordServers");
        }
    }
}
