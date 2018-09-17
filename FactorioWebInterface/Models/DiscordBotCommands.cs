using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public class DiscordBotCommands
    {
        private readonly DiscordBot _discordBot;

        public DiscordBotCommands(DiscordBot discordBot)
        {
            _discordBot = discordBot;

            var c = _discordBot.DiscordClient.GetCommandsNext();
            c.CommandErrored += CommandErrored;
        }

        private async Task CommandErrored(CommandErrorEventArgs e)
        {
            string commandName = e.Command?.Name;
            if (commandName == null)
            {
                await e.Context.RespondAsync($"Unknow command name see ;;help for command information.");
            }
            else
            {
                await e.Context.RespondAsync($"Invalid use of {commandName} see ;;help {commandName} for help with this command.");
            }
        }

        [Command("setserver")]
        [RequireUserPermissions(DSharpPlus.Permissions.ManageChannels)]
        [Description("Connects the factorio server to this channel.")]
        public async Task SetServer(CommandContext ctx, [Description("The Factorio server ID eg 7.")] string serverId)
        {
            bool success = await _discordBot.SetServer(serverId, ctx.Channel.Id);
            if (success)
            {
                await ctx.RespondAsync($"Facotrio server {serverId} has been connected to this channel");
            }
            else
            {
                await ctx.RespondAsync($"Error connecting the facotrio server {serverId} to this channel");
            }            
        }

        [Command("unset")]
        [RequireUserPermissions(DSharpPlus.Permissions.ManageChannels)]
        [Description("Disconnects the currently connected factorio server from this channel.")]
        public async Task UnSetServer(CommandContext ctx)
        {
            string serverId = await _discordBot.UnSetServer(ctx.Channel.Id);
            if (serverId != null)
            {
                await ctx.RespondAsync($"Facotrio server {serverId} has been disconnected from this channel");
            }
            else
            {
                await ctx.RespondAsync($"Error disconnecting the facotrio server from this channel");
            }
        }
    }
}
