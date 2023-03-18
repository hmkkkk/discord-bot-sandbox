using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using hmkSandboxBot.Constants;
using hmkSandboxBot.Models;
using System.Text;

namespace hmkSandboxBot.Helpers
{
    public static class LavalinkHelpers
    {
        public static bool CheckForLavalinkConnectedNodes(CommandContext ctx, LavalinkExtension lava)
        {
            if (!lava.ConnectedNodes.Any())
            {
                return false;
            }

            return true;
        }

        public static bool CheckForLavalinkConnection(CommandContext ctx, LavalinkGuildConnection conn)
        {
            if (conn == null)
            {
                return false;
            }

            return true;
        }

        public static bool CheckForIfUserIsInTheChannel(CommandContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                return false;
            }

            return true;
        }

        public static bool CheckForChannelType(CommandContext ctx, DiscordChannel channel)
        {
            if (channel.Type != ChannelType.Voice)
            {
                return false;
            }

            return true;
        }
    }
}
