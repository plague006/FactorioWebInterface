using DSharpPlus.Entities;

namespace FactorioWebInterface.Models
{
    public class ServerMessageEventArgs
    {
        public ServerMessageEventArgs(string serverId, DiscordUser user, string message)
        {
            ServerId = serverId;
            User = user;
            Message = message;
        }

        public string ServerId { get; }
        public DiscordUser User { get; }
        public string Message { get; }
    }
}