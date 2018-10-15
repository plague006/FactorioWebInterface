using FactorioWebInterface.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Linq;

namespace FactorioWebInterface.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IDiscordBot _discordBot;

        public IndexModel(IConfiguration configuration, IDiscordBot discordBot)
        {
            _configuration = configuration;
            _discordBot = discordBot;
        }

        public void OnGet()
        {
        }

        public IActionResult OnPostRemoveRoles()
        {
            RemoveRoles();

            return Page();
        }

        private async void RemoveRoles()
        {
            var client = _discordBot.DiscordClient;
            var guildId = ulong.Parse(_configuration[Constants.GuildIDKey]);
            var guild = await client.GetGuildAsync(guildId);
            var testRole = guild.Roles.FirstOrDefault(r => r.Name == "testRole");

            var membersCollection = await guild.GetAllMembersAsync();
            var members = membersCollection.ToArray();

            var totalCount = members.Length;
            int count = 0;

            foreach (var m in members)
            {
                if (m.Roles.Contains(testRole))
                {
                    await m.RevokeRoleAsync(testRole);
                }

                count++;
                Debug.WriteLine($"{count} / {totalCount}");
            }
        }
    }
}
