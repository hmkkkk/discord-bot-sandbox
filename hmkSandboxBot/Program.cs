
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using hmkSandboxBot.Commands;
using hmkSandboxBot.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace hmkSandboxBot;
class Program
{
    static void Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args).Build();
        IConfiguration config = host.Services.GetRequiredService<IConfiguration>();

        BotConfig botConfig = config.GetRequiredSection("BotConfig").Get<BotConfig>();

        MainAsync(botConfig).GetAwaiter().GetResult();
    }

    static async Task MainAsync(BotConfig botConfig)
    {
        var discord = new DiscordClient(new DiscordConfiguration()
        {
            Token = botConfig.Token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.All,
            MinimumLogLevel = LogLevel.Debug
        });

        var endpoint = new ConnectionEndpoint
        {
            Hostname = "127.0.0.1", // From your server configuration.
            Port = 2333 // From your server configuration
        };

        var commands = discord.UseCommandsNext(new CommandsNextConfiguration
        {
            StringPrefixes = new[] { "h!" }
        });
        commands.RegisterCommands<BotCommands>();
        commands.RegisterCommands<MusicCommands>();

        var lavalinkConfig = new LavalinkConfiguration
        {
            Password = "youshallnotpass", // From your server configuration.
            RestEndpoint = endpoint,
            SocketEndpoint = endpoint
        };
        var lavalink = discord.UseLavalink();

        await discord.ConnectAsync();
        await lavalink.ConnectAsync(lavalinkConfig); // Make sure this is after Discord.ConnectAsync(). 

        await Task.Delay(-1);
    }
}
