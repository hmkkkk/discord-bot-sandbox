using DSharpPlus.Entities;

namespace hmkSandboxBot.Helpers
{
    public static class BotHelpers
    {
        public static async Task<DiscordMessage> CreateDiscordMessage(string content, DiscordChannel channel)
        {
            return await new DiscordMessageBuilder()
                .WithContent(content)
                .SendAsync(channel);
        }
    }
}
