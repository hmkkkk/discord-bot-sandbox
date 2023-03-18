using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using hmkSandboxBot.Constants;
using hmkSandboxBot.Helpers;

namespace hmkSandboxBot.Commands
{
    public class BotCommands : BaseCommandModule
    {
        [Command("hello")]
        public async Task GreetCommand(CommandContext context)
        {
            await BotHelpers.SendDiscordMessageWithEmbed("",
                    $"Hello {context.Message.Author.Mention} ❤️",
                    HexColorConstants.Pink,
                    null,
                    context.Message.Channel);
        }
    }
}
