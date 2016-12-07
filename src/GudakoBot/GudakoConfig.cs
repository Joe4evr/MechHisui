using System.Collections.Generic;
using Discord.Addons.SimpleConfig;

namespace GudakoBot
{
    public sealed class GudakoConfig : IConfig
    {
        public ulong OwnerId { get; set; }

        public string LoginToken { get; set; }

        public ulong FgoGeneral { get; set; }

        public IEnumerable<string> Lines { get; set; }
    }
}
