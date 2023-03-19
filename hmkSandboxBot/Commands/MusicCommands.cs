using AutoMapper;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using hmkSandboxBot.Constants;
using hmkSandboxBot.Helpers;
using hmkSandboxBot.Models;

namespace hmkSandboxBot.Commands
{
    public class MusicCommands : BaseCommandModule
    {
        private Queue<LavalinkTrackExtended> _tracks = new Queue<LavalinkTrackExtended>();
        private readonly int _pageSize = 5;
        private readonly IMapper _mapper;

        public MusicCommands(IMapper mapper)
        {
            _mapper = mapper;
        }

        [Command("join")]
        public async Task Join(CommandContext ctx)
        {
            var lava = ctx.Client.GetLavalink();

            if (!LavalinkHelpers.CheckForLavalinkConnectedNodes(ctx, lava)) 
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                    "The Lavalink connection is not established",
                    HexColorConstants.Red,
                    null,
                    ctx.Message.Channel);

                return;
            }

            if (!LavalinkHelpers.CheckForChannelType(ctx, ctx.Member.VoiceState.Channel))
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                    "Not a valid voice channel.",
                    HexColorConstants.Red,
                    null,
                    ctx.Message.Channel);

                return;
            } 

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
                        var track = _tracks.Dequeue();
                        await conn.PlayAsync(track);
                        await BotHelpers.SendDiscordMessageWithEmbed("Now playing",
                            MessageHelpers.CreateMessageForNowPlayingTrack(track),
                            HexColorConstants.Blue,
                            track.Uri,
                            ctx.Message.Channel);
                    }
                };
            }
        }

        [Command("leave")]
        public async Task Leave(CommandContext ctx)
        {
            var lava = ctx.Client.GetLavalink();

            if (!LavalinkHelpers.CheckForLavalinkConnectedNodes(ctx, lava))
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                    "The Lavalink connection is not established",
                    HexColorConstants.Red,
                    null,
                    ctx.Message.Channel);

                return;
            }

            if (!LavalinkHelpers.CheckForChannelType(ctx, ctx.Member.VoiceState.Channel))
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                    "Not a valid voice channel.",
                    HexColorConstants.Red,
                    null,
                    ctx.Message.Channel);

                return;
            }

            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (!LavalinkHelpers.CheckForLavalinkConnection(ctx, conn))
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                    "Lavalink is not connected.",
                    HexColorConstants.Red,
                    null,
                    ctx.Message.Channel);

                return;
            };

            await conn.DisconnectAsync();

            await BotHelpers.SendDiscordMessageWithEmbed("Bye ❤️",
                    $"",
                    HexColorConstants.Pink,
                    null,
                    ctx.Message.Channel);

            _tracks = new Queue<LavalinkTrackExtended>(); // get rid of old queue
        }

        [Command("play")]
        public async Task Play(CommandContext ctx, [RemainingText] string search)
        {
            if (!LavalinkHelpers.CheckForIfUserIsInTheChannel(ctx)) 
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                    "You are not in a voice channel.",
                    HexColorConstants.Red,
                    null,
                    ctx.Message.Channel);

                return;
            };

            if (string.IsNullOrWhiteSpace(search))
            {
                await BotHelpers.SendDiscordMessageWithEmbed("Search info missing",
                        $"Some search info is required {ctx.User.Mention}. \n\nUsage: h!play <song name or url>",
                        HexColorConstants.Red,
                        null,
                        ctx.Message.Channel);

                return;
            }

            await Join(ctx);

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (!LavalinkHelpers.CheckForLavalinkConnection(ctx, conn))
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                    "Lavalink is not connected.",
                    HexColorConstants.Red,
                    null,
                    ctx.Message.Channel);

                return;
            };

            if (!ctx.Member.VoiceState.Channel.Id.Equals(conn?.Channel?.Id))
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                        $"You must be in the same voice channel as the bot to play music {ctx.User.Mention}.",
                        HexColorConstants.Red,
                        null,
                        ctx.Message.Channel);

                return;
            }

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
                await BotHelpers.SendDiscordMessageWithEmbed("",
                        $"Track search failed.",
                        HexColorConstants.Red,
                        null,
                        ctx.Message.Channel);

                return;
            }

            if (loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                        $"No matches found for {search}",
                        HexColorConstants.Red,
                        null,
                        ctx.Message.Channel);

                return;
            }

            if (string.IsNullOrEmpty(loadResult.PlaylistInfo.Name))
            {
                var trackFound = _mapper.Map<LavalinkTrackExtended>(loadResult.Tracks.First());
                trackFound.UserMention = ctx.User.Mention;
                _tracks.Enqueue(trackFound);

                if (conn.CurrentState.CurrentTrack != null)
                {
                    await BotHelpers.SendDiscordMessageWithEmbed("",
                            $"{trackFound.Title} added to queue.",
                            HexColorConstants.Blue,
                            trackFound.Uri,
                            ctx.Message.Channel);
                }
            }
            else
            {
                foreach (var track in loadResult.Tracks)
                {
                    var trackExtended = _mapper.Map<LavalinkTrackExtended>(track);
                    trackExtended.UserMention = ctx.User.Mention;
                    _tracks.Enqueue(trackExtended);
                }
                await BotHelpers.SendDiscordMessageWithEmbed($"{loadResult.Tracks.Count()} tracks added to queue",
                        $"Current queue length: {_tracks.Count} tracks.",
                        HexColorConstants.Blue,
                        null,
                        ctx.Message.Channel);
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                var track = _tracks.Dequeue();

                await conn.PlayAsync(track);
                await BotHelpers.SendDiscordMessageWithEmbed("Now playing",
                        MessageHelpers.CreateMessageForNowPlayingTrack(track),
                        HexColorConstants.Blue,
                        track.Uri,
                        ctx.Message.Channel);
            }

        }

        [Command("search")]
        public async Task Search(CommandContext ctx, [RemainingText] string search)
        {
            if (!LavalinkHelpers.CheckForIfUserIsInTheChannel(ctx))
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                    "You are not in a voice channel.",
                    HexColorConstants.Red,
                    null,
                    ctx.Message.Channel);

                return;
            };

            if (string.IsNullOrWhiteSpace(search))
            {
                await BotHelpers.SendDiscordMessageWithEmbed("Search info missing",
                        $"Some search info is required {ctx.User.Mention}. \n\nUsage: h!play <song name or url>",
                        HexColorConstants.Red,
                        null,
                        ctx.Message.Channel);

                return;
            }

            await Join(ctx);

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (!LavalinkHelpers.CheckForLavalinkConnection(ctx, conn))
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                    "Lavalink is not connected.",
                    HexColorConstants.Red,
                    null,
                    ctx.Message.Channel);

                return;
            };

            if (!ctx.Member.VoiceState.Channel.Id.Equals(conn?.Channel?.Id))
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                        $"You must be in the same voice channel as the bot to play music {ctx.User.Mention}.",
                        HexColorConstants.Red,
                        null,
                        ctx.Message.Channel);

                return;
            }

            var loadResult = await node.Rest.GetTracksAsync(search);

            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed)
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                        $"Track search failed.",
                        HexColorConstants.Red,
                        null,
                        ctx.Message.Channel);

                return;
            }

            if (loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                        $"No matches found for {search}",
                        HexColorConstants.Red,
                        null,
                        ctx.Message.Channel);

                return;
            }

            bool nextMessageFlag = true;
            int currentPage = 1;

            var trackResult = loadResult.Tracks.ToList();

            while (nextMessageFlag)
            {
                var trackTitlesToShowcase = trackResult.Select(x => x.Title)
                    .Skip((currentPage - 1) * _pageSize)
                    .Take(_pageSize)
                    .ToList();

                await BotHelpers.SendDiscordMessageWithEmbed("Search",
                        MessageHelpers.CreateMessageForSearchCommand(currentPage, _pageSize, trackTitlesToShowcase, trackResult.Count),
                        HexColorConstants.Blue,
                        null,
                        ctx.Message.Channel);

                var nextMessage = await ctx.Message.GetNextMessageAsync();

                if (nextMessage.TimedOut)
                {
                    await BotHelpers.SendDiscordMessageWithEmbed("Search",
                            "Search request timed out.",
                            HexColorConstants.Red,
                            null,
                            ctx.Message.Channel);

                    return;
                }

                string nextMessageContent = nextMessage.Result.Content.Trim();

                nextMessageFlag = nextMessageContent == "next" || nextMessageContent == "prev";

                if (nextMessageContent == "next") currentPage++;

                if (nextMessageContent == "prev" && currentPage > 1) currentPage--;

                if (Int32.TryParse(nextMessageContent, out int trackNumber))
                {
                    if (trackNumber <= 0 || trackNumber > trackResult.Count) 
                    {
                        await BotHelpers.SendDiscordMessageWithEmbed("Search",
                            "Invalid track number. Search request failed.",
                            HexColorConstants.Red,
                            null,
                            ctx.Message.Channel);

                        return;
                    }

                    var trackToPlay = _mapper.Map<LavalinkTrackExtended>(trackResult[trackNumber - 1]);

                    await conn.PlayAsync(trackToPlay);
                    await BotHelpers.SendDiscordMessageWithEmbed("Now playing",
                        MessageHelpers.CreateMessageForNowPlayingTrack(trackToPlay),
                        HexColorConstants.Blue,
                        trackToPlay.Uri,
                        ctx.Message.Channel);

                    return;
                }
            }

            await BotHelpers.SendDiscordMessageWithEmbed("Search",
                            "Search request failed.",
                            HexColorConstants.Red,
                            null,
                            ctx.Message.Channel);

        }

        [Command("pause")]
        public async Task Pause(CommandContext ctx)
        {
            if (!LavalinkHelpers.CheckForIfUserIsInTheChannel(ctx))
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                    "You are not in a voice channel.",
                    HexColorConstants.Red,
                    null,
                    ctx.Message.Channel);
                
                return;
            };

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (!LavalinkHelpers.CheckForLavalinkConnection(ctx, conn))
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                    "Lavalink is not connected.",
                    HexColorConstants.Red,
                    null,
                    ctx.Message.Channel);

                return;
            };

            if (conn.CurrentState.CurrentTrack == null)
            {
                await BotHelpers.SendDiscordMessageWithEmbed("Pause",
                        $"There is nothing to pause.",
                        HexColorConstants.Red,
                        null,
                        ctx.Message.Channel);
                return;
            }

            await conn.PauseAsync();
        }

        [Command("resume")]
        public async Task Resume(CommandContext ctx)
        {
            if (!LavalinkHelpers.CheckForIfUserIsInTheChannel(ctx))
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                    "You are not in a voice channel.",
                    HexColorConstants.Red,
                    null,
                    ctx.Message.Channel);

                return;
            };

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (!LavalinkHelpers.CheckForLavalinkConnection(ctx, conn))
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                    "Lavalink is not connected.",
                    HexColorConstants.Red,
                    null,
                    ctx.Message.Channel);

                return;
            };

            if (conn.CurrentState.CurrentTrack == null)
            {
                await BotHelpers.SendDiscordMessageWithEmbed("Resume",
                        $"There is no track loaded to resume.",
                        HexColorConstants.Red,
                        null,
                        ctx.Message.Channel);

                return;
            }

            await conn.ResumeAsync();
        }

        [Command("stop")]
        public async Task Stop(CommandContext ctx)
        {
            if (!LavalinkHelpers.CheckForIfUserIsInTheChannel(ctx))
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                    "You are not in a voice channel.",
                    HexColorConstants.Red,
                    null,
                    ctx.Message.Channel);

                return;
            };

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (!LavalinkHelpers.CheckForLavalinkConnection(ctx, conn))
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                    "Lavalink is not connected.",
                    HexColorConstants.Red,
                    null,
                    ctx.Message.Channel);

                return;
            };

            if (conn.CurrentState.CurrentTrack == null)
            {
                await BotHelpers.SendDiscordMessageWithEmbed("Stop",
                        $"There are no tracks loaded.",
                        HexColorConstants.Red,
                        null,
                        ctx.Message.Channel);

                return;
            }

            _tracks = new Queue<LavalinkTrackExtended>();

            await conn.StopAsync();
            await BotHelpers.SendDiscordMessageWithEmbed("Bot has been stopped",
                    $"The queue has been reset.",
                    HexColorConstants.Blue,
                    null,
                    ctx.Message.Channel);
        }

        [Command("skip")]
        public async Task Skip(CommandContext ctx)
        {
            if (!LavalinkHelpers.CheckForIfUserIsInTheChannel(ctx))
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                    "You are not in a voice channel.",
                    HexColorConstants.Red,
                    null,
                    ctx.Message.Channel);

                return;
            };

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (!LavalinkHelpers.CheckForLavalinkConnection(ctx, conn))
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                    "Lavalink is not connected.",
                    HexColorConstants.Red,
                    null,
                    ctx.Message.Channel);

                return;
            };

            if (conn.CurrentState.CurrentTrack == null)
            {
                await BotHelpers.SendDiscordMessageWithEmbed("Skip",
                        $"There are no tracks loaded.",
                        HexColorConstants.Red,
                        null,
                        ctx.Message.Channel);

                return;
            }

            await BotHelpers.SendDiscordMessageWithEmbed($"Song skipped.",
                    $"{conn.CurrentState.CurrentTrack.Title} has been skipped",
                    HexColorConstants.Blue,
                    conn.CurrentState.CurrentTrack.Uri,
                    ctx.Message.Channel);

            if (_tracks.Count > 0)
            {
                var nextTrack = _tracks.Dequeue();
                await conn.PlayAsync(nextTrack);
                await BotHelpers.SendDiscordMessageWithEmbed("Now playing",
                    MessageHelpers.CreateMessageForNowPlayingTrack(nextTrack), 
                    HexColorConstants.Blue,
                    nextTrack.Uri,
                    ctx.Message.Channel);
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
                await BotHelpers.SendDiscordMessageWithEmbed("Shuffle",
                        $"There are no tracks in queue to shuffle.",
                        HexColorConstants.Red,
                        null,
                        ctx.Message.Channel);

                return;
            }

            var rng = new Random();

            _tracks = new Queue<LavalinkTrackExtended>(_tracks.OrderBy(x => rng.Next()));

            await BotHelpers.SendDiscordMessageWithEmbed("Shuffle",
                    $"The queue has been shuffled.",
                    HexColorConstants.Blue,
                    null,
                    ctx.Message.Channel);
        }

        [Command("queue")]
        public async Task Queue(CommandContext ctx, [RemainingText] int currentPage = 1)
        {
            if (_tracks.Count == 0)
            {
                await BotHelpers.SendDiscordMessageWithEmbed("Queue",
                        $"There are no tracks in queue.",
                        HexColorConstants.Red,
                        null,
                        ctx.Message.Channel);

                return;
            }

            var trackTitlesToShowcase = _tracks.Select(x => x.Title)
                    .Skip((currentPage - 1) * _pageSize)
                    .Take(_pageSize)
                    .ToList();

            await BotHelpers.SendDiscordMessageWithEmbed("Queue",
                    MessageHelpers.CreateMessageForQueueCommand(currentPage, _pageSize, trackTitlesToShowcase, _tracks.Count),
                    HexColorConstants.Blue,
                    null,
                    ctx.Message.Channel);
        }

        [Command("current")]
        public async Task CurrentSong(CommandContext ctx)
        {
            if (!LavalinkHelpers.CheckForIfUserIsInTheChannel(ctx))
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                    "You are not in a voice channel.",
                    HexColorConstants.Red,
                    null,
                    ctx.Message.Channel);

                return;
            };

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (!LavalinkHelpers.CheckForLavalinkConnection(ctx, conn))
            {
                await BotHelpers.SendDiscordMessageWithEmbed("",
                    "Lavalink is not connected.",
                    HexColorConstants.Red,
                    null,
                    ctx.Message.Channel);

                return;
            };

            if (conn.CurrentState.CurrentTrack == null)
            {
                await BotHelpers.SendDiscordMessageWithEmbed("Now playing",
                        $"There are no tracks loaded.",
                        HexColorConstants.Red,
                        null,
                        ctx.Message.Channel);

                return;
            }

            await BotHelpers.SendDiscordMessageWithEmbed("Now playing",
                    $"{conn.CurrentState.CurrentTrack.Title}",
                    HexColorConstants.Blue,
                    conn.CurrentState.CurrentTrack.Uri,
                    ctx.Message.Channel);
        }

    }
}
