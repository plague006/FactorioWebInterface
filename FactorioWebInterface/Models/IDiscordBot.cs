using DSharpPlus;
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
        Task SendEmbedToFactorioChannel(string serverId, string data);
        Task SendToFactorioAdminChannel(string data);

        event EventHandler<IDiscordBot, ServerMessageEventArgs> FactorioDiscordDataReceived;
    }
}
