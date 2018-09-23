using DSharpPlus;
using DSharpPlus.Entities;
using FactorioWebInterface.Utils;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public interface IDiscordBot
    {
        DiscordClient DiscordClient { get; }
        Task<bool> IsAdminRoleAsync(string userId);
        Task<bool> IsAdminRoleAsync(ulong userId);
        Task SendToFactorioChannel(string serverId, string data);
        Task SendEmbedToFactorioChannel(string serverId, DiscordEmbed embed);
        Task SendToFactorioAdminChannel(string data);
        Task SendEmbedToFactorioAdminChannel(DiscordEmbed embed);

        event EventHandler<IDiscordBot, ServerMessageEventArgs> FactorioDiscordDataReceived;
    }
}
