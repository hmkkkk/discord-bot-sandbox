using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Lavalink;
using hmkSandboxBot.Helpers;
using System.Text;

namespace hmkSandboxBot.Commands
{
    public class MusicCommands : BaseCommandModule
    {
        private Queue<LavalinkTrack> _tracks = new Queue<LavalinkTrack>();
        private readonly int _pageSize = 5;

        [Command("join")]
        public async Task Join(CommandContext ctx)
        {
            var lava = ctx.Client.GetLavalink();

            if (!await LavalinkHelpers.CheckForLavalinkConnectedNodes(ctx, lava)) return;

            if (!await LavalinkHelpers.CheckForChannelType(ctx, ctx.Member.VoiceState.Channel)) return;

            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (!ctx.Member.VoiceState.Channel.Id.Equals(conn?.Channel?.Id))
            {
                await node.ConnectAsync(ctx.Member.VoiceState.Channel);

                conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

                conn.PlaybackFinished += async (conn, args) =>
                {
                    if (_tracks.Count == 0) return;

                    if (conn.CurrentState.CurrentTrack == null)
                    {
                        await conn.PlayAsync(_tracks.Dequeue());
                        await BotHelpers.CreateDiscordMessage($"Now playing {conn.CurrentState.CurrentTrack.Title}", ctx.Message.Channel);
                    }
                };
            }
        }

        [Command("leave")]
        public async Task Leave(CommandContext ctx)
        {
            var lava = ctx.Client.GetLavalink();

            if (!await LavalinkHelpers.CheckForLavalinkConnectedNodes(ctx, lava)) return;

            if (!await LavalinkHelpers.CheckForChannelType(ctx, ctx.Member.VoiceState.Channel)) return;

            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (!await LavalinkHelpers.CheckForLavalinkConnection(ctx, conn)) return;

            await conn.DisconnectAsync();

            _tracks = new Queue<LavalinkTrack>(); // get rid of old queue
        }

        [Command("play")]
        public async Task Play(CommandContext ctx, [RemainingText] string search)
        {
            if (!await LavalinkHelpers.CheckForIfUserIsInTheChannel(ctx)) return;

            if (string.IsNullOrWhiteSpace(search))
            {
                await ctx.RespondAsync($"Some search info is required. \n\nUsage: h!play <song name or url>");
                return;
            }

            await Join(ctx);

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (!await LavalinkHelpers.CheckForLavalinkConnection(ctx, conn)) return;

            Uri uriResult;
            bool isSearchValidUri = Uri.TryCreate(search, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            var loadResult = new LavalinkLoadResult();

            if (isSearchValidUri)
            {
                loadResult = await node.Rest.GetTracksAsync(uriResult);
            }
            else
            {
                loadResult = await node.Rest.GetTracksAsync(search);
            }

            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed)
            {
                await ctx.RespondAsync($"Track search failed.");
                return;
            }

            if (loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.RespondAsync($"No matches found for {search}.");
                return;
            }

            if (string.IsNullOrEmpty(loadResult.PlaylistInfo.Name))
            {
                var trackFound = loadResult.Tracks.First();
                _tracks.Enqueue(trackFound);

                if (conn.CurrentState.CurrentTrack != null)
                {
                    await ctx.RespondAsync($"{trackFound.Title} added to queue.");
                }
            }
            else
            {
                foreach (var track in loadResult.Tracks)
                {
                    _tracks.Enqueue(track);
                }
                await ctx.RespondAsync($"Added {loadResult.Tracks.Count()} tracks to queue. \n\nCurrent queue length: {_tracks.Count} tracks.");
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await conn.PlayAsync(_tracks.Dequeue());
                await BotHelpers.CreateDiscordMessage($"Now playing {conn.CurrentState.CurrentTrack.Title}", ctx.Message.Channel);
            }

        }

        [Command("pause")]
        public async Task Pause(CommandContext ctx)
        {
            if (!await LavalinkHelpers.CheckForIfUserIsInTheChannel(ctx)) return;

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (!await LavalinkHelpers.CheckForLavalinkConnection(ctx, conn)) return;

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            await conn.PauseAsync();
        }

        [Command("resume")]
        public async Task Resume(CommandContext ctx)
        {
            if (!await LavalinkHelpers.CheckForIfUserIsInTheChannel(ctx)) return;

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (!await LavalinkHelpers.CheckForLavalinkConnection(ctx, conn)) return;

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            await conn.ResumeAsync();
        }

        [Command("stop")]
        public async Task Stop(CommandContext ctx)
        {
            if (!await LavalinkHelpers.CheckForIfUserIsInTheChannel(ctx)) return;

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (!await LavalinkHelpers.CheckForLavalinkConnection(ctx, conn)) return;

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            await conn.StopAsync();
            await ctx.RespondAsync("Bot has been stopped. The queue has been reset.");

            _tracks = new Queue<LavalinkTrack>();
        }

        [Command("skip")]
        public async Task Skip(CommandContext ctx)
        {
            if (!await LavalinkHelpers.CheckForIfUserIsInTheChannel(ctx)) return;

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (!await LavalinkHelpers.CheckForLavalinkConnection(ctx, conn)) return;

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            await ctx.RespondAsync($"Skipped {conn.CurrentState.CurrentTrack.Title}.");

            if (_tracks.Count > 0)
            {
                var nextTrack = _tracks.Dequeue();
                await conn.PlayAsync(nextTrack);
                await BotHelpers.CreateDiscordMessage($"Now playing {nextTrack.Title}", ctx.Message.Channel);
            }
            else
            {
                await conn.StopAsync();
            }
         
        }

        [Command("shuffle")]
        public async Task Shuffle(CommandContext ctx)
        {
            if (_tracks.Count == 0)
            {
                await ctx.RespondAsync($"There are no tracks in queue to shuffle.");
                return;
            }

            var rng = new Random();

            _tracks = new Queue<LavalinkTrack>(_tracks.OrderBy(x => rng.Next()));

            await ctx.RespondAsync($"The queue has been shuffled.");
        }

        [Command("queue")]
        public async Task Queue(CommandContext ctx, int currentPage = 1)
        {
            if (_tracks.Count == 0)
            {
                await ctx.RespondAsync($"There are no tracks in queue.");
                return;
            }

            var trackTitlesToShowcase = _tracks.Select(x => x.Title)
                    .Skip((currentPage - 1) * _pageSize) // todo: implement pagination
                    .Take(_pageSize)
                    .ToList();

            var sb = new StringBuilder();

            for (int i = 0; i < _pageSize; i++)
            {
                sb.AppendLine($"{i + 1}. {trackTitlesToShowcase[i]}");
            }

            await ctx.RespondAsync(sb.ToString());
        }
    }
}
