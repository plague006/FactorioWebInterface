using DSharpPlus;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public class Bot
    {
        public static DiscordClient Discord { get; private set; }

        public static async Task StartAsync()
        {
            Discord = new DiscordClient(new DiscordConfiguration
            {
                Token = "NDg3MjkwNTg0MDkyOTAxMzg2.DnQtHg.7_FwwMhV0Xgd2coDym6p6CLwgmg",
                TokenType = TokenType.Bot
            });

            Discord.MessageCreated += async e =>
            {
                if (e.Message.Content.ToLower().StartsWith("!ping"))
                    await e.Message.RespondAsync("pong!");                
            };            
            
            await Discord.ConnectAsync();            
        }
    }
}
