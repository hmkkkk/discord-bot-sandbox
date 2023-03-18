using DSharpPlus.Entities;

namespace hmkSandboxBot.Helpers
{
    public static class BotHelpers
    {
        public static async Task<DiscordMessage> SendDiscordMessageWithEmbed(string title,
            string content,
            string color,
            Uri url,
            DiscordChannel channel)
        {
            DiscordEmbed discordEmbed = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(color))
                .WithTitle(title)
                .WithDescription(content)
                .WithUrl(url)
                .Build();
                
            return await new DiscordMessageBuilder()
                .WithEmbed(discordEmbed)
                .SendAsync(channel);
        }

        public static async Task<DiscordMessage> SendDiscordMessage(string content, DiscordChannel channel)
        {
            return await new DiscordMessageBuilder()
                .WithContent(content)
                .SendAsync(channel);
        }
    }
}
