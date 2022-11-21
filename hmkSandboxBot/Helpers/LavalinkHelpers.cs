using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;

namespace hmkSandboxBot.Helpers
{
    public static class LavalinkHelpers
    {
        public static async Task<bool> CheckForLavalinkConnectedNodes(CommandContext ctx, LavalinkExtension lava)
        {
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.RespondAsync("The Lavalink connection is not established");
                return false;
            }

            return true;
        }

        public static async Task<bool> CheckForLavalinkConnection(CommandContext ctx, LavalinkGuildConnection conn)
        {
            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return false;
            }

            return true;
        }

        public static async Task<bool> CheckForIfUserIsInTheChannel(CommandContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return false;
            }

            return true;
        }

        public static async Task<bool> CheckForChannelType(CommandContext ctx, DiscordChannel channel)
        {
            if (channel.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("Not a valid voice channel.");
                return false;
            }

            return true;
        }
    }
}
