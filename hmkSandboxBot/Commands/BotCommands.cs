using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace hmkSandboxBot.Commands
{
    public class BotCommands : BaseCommandModule
    {
        [Command("hello")]
        public async Task GreetCommand(CommandContext context)
        {
            await context.RespondAsync($"Hello {context.Message.Author.Mention}");
        }
    }
}
