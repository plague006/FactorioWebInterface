using DSharpPlus;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public class DiscordBot : IDiscordBot
    {
        private readonly IConfiguration _configuration;
        private readonly ulong guildId;
        private readonly ulong adminRoleId;

        public DiscordClient DiscordClient { get; private set; }

        public DiscordBot(IConfiguration configuration)
        {
            _configuration = configuration;
            guildId = ulong.Parse(_configuration[Constants.GuildIDKey]);
            adminRoleId = ulong.Parse(_configuration[Constants.AdminRoleIDKey]);
            Init();
        }

        private void Init()
        {
            DiscordClient = new DiscordClient(new DiscordConfiguration
            {
                Token = _configuration[Constants.DiscordBotTokenKey],
                TokenType = TokenType.Bot
            });

            DiscordClient.MessageCreated += async e =>
            {
                if (e.Message.Content.ToLower().StartsWith("!hamping"))
                {
                    await e.Message.RespondAsync("hampong!");
                }

                if (e.Message.Content.ToLower().StartsWith("!humping"))
                {
                    await e.Message.RespondAsync("humpong!");
                }
            };

            DiscordClient.ConnectAsync().GetAwaiter().GetResult();


        }

        /// <summary>
        /// Returns a boolean for if the discord user has the admin role in the Redmew guild.
        /// </summary>
        /// <param name="userId">The discord user's id.</param>        
        public async Task<bool> IsAdminRoleAsync(ulong userId)
        {
            var guild = await DiscordClient.GetGuildAsync(guildId);
            var memeber = await guild.GetMemberAsync(userId);

            if (memeber == null)
                return false;

            var role = memeber.Roles.FirstOrDefault(x => x.Id == adminRoleId);

            return role != null;
        }

        /// <summary>
        /// Returns a boolean for if the discord user has the admin role in the Redmew guild.
        /// </summary>
        /// <param name="userId">The discord user's id.</param> 
        public Task<bool> IsAdminRoleAsync(string userId)
        {
            if (ulong.TryParse(userId, out ulong id))
                return IsAdminRoleAsync(id);
            else
                return Task.FromResult(false);
        }
    }
}
