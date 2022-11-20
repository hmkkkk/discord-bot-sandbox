using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using Emzi0767.Utilities;
using hmkSandboxBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
