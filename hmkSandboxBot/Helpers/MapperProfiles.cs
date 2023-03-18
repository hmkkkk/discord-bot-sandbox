using AutoMapper;
using DSharpPlus.Lavalink;
using hmkSandboxBot.Models;

namespace hmkSandboxBot.Helpers
{
    public class MapperProfiles : Profile
    {
        public MapperProfiles()
        {
            CreateMap<LavalinkTrack, LavalinkTrackExtended>();
        }
    }
}
