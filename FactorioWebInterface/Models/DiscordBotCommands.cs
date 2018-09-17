using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public class DiscordBotCommands
    {
        private static readonly DiscordColor infoColor = new DiscordColor(0, 127, 255);

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
                var embed = new DiscordEmbedBuilder()
                {
                    Description = $"Unknow command name see ;;help for command information.",
                    Color = DiscordColor.Red
                }
                .Build();

                await e.Context.RespondAsync(embed: embed);
            }
            else
            {
                var embed = new DiscordEmbedBuilder()
                {
                    Description = $"Invalid use of {commandName} see ;;help {commandName} for help with this command.",
                    Color = DiscordColor.Red
                }
                .Build();

                await e.Context.RespondAsync(embed: embed);
            }
        }

        [Command("ping")]
        [Description("Pings the bot.")]
        [Hidden]
        public async Task Ping(CommandContext ctx)
        {
            var diff = DateTimeOffset.Now - ctx.Message.Timestamp;

            var embed = new DiscordEmbedBuilder()
            {
                Title = $"pong in {diff.TotalMilliseconds}ms",
                Color = infoColor
            }
            .Build();

            await ctx.RespondAsync(embed: embed);
        }

        [Command("setserver")]
        [RequireUserPermissions(DSharpPlus.Permissions.ManageChannels)]
        [Description("Connects the factorio server to this channel.")]
        public async Task SetServer(CommandContext ctx, [Description("The Factorio server ID eg 7.")] string serverId)
        {
            bool success = await _discordBot.SetServer(serverId, ctx.Channel.Id);
            if (success)
            {
                var embed = new DiscordEmbedBuilder()
                {
                    Description = $"Facotrio server {serverId} has been connected to this channel",
                    Color = DiscordColor.Green
                }
                .Build();

                await ctx.RespondAsync(embed: embed);
            }
            else
            {
                var embed = new DiscordEmbedBuilder()
                {
                    Description = $"Error connecting the facotrio server {serverId} to this channel",
                    Color = DiscordColor.Red
                }
                .Build();

                await ctx.RespondAsync(embed: embed);
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
                var embed = new DiscordEmbedBuilder()
                {
                    Description = $"Facotrio server {serverId} has been disconnected from this channel",
                    Color = DiscordColor.Green
                }
                .Build();

                await ctx.RespondAsync(embed: embed);
            }
            else
            {
                var embed = new DiscordEmbedBuilder()
                {
                    Description = $"Error disconnecting the facotrio server from this channel",
                    Color = DiscordColor.Red
                }
                .Build();

                await ctx.RespondAsync(embed: embed);
            }
        }
    }
}
