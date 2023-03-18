using hmkSandboxBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hmkSandboxBot.Helpers
{
    public static class MessageHelpers
    {
        public static string CreateMessageForQueueCommand(int currentPage, int pageSize, List<string> trackTitlesToShowcase, int tracksCount)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < trackTitlesToShowcase.Count; i++)
            {
                sb.AppendLine($"{i + 1}. {trackTitlesToShowcase[i]}");
            }

            int pageCount = tracksCount / pageSize;
            if (tracksCount % pageSize != 0) pageCount++;

            sb.AppendLine($"Page {currentPage}/{pageCount}");

            return sb.ToString();
        }

        public static string CreateMessageForNowPlayingTrack(LavalinkTrackExtended track)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"{track.Title} by {track.Author}");
            sb.AppendLine($"`[{track.Position.ToString(@"mm\:ss")} / {track.Length.ToString(@"mm\:ss")}]`");
            sb.AppendLine();
            sb.AppendLine($"Requested by {track.UserMention}");

            return sb.ToString();
        }
    }
}
