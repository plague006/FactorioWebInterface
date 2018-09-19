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

        event EventHandler<IDiscordBot, ServerMessageEventArgs> FactorioDiscordDataReceived;
    }
}
