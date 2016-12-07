using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MechHisui.Superfight
{
    public sealed class SuperfightConfig
    {
        public IEnumerable<string> Characters { get; }
        public IEnumerable<string> Abilities { get; }
        public IEnumerable<string> Locations { get; }

        public SuperfightConfig(
            IEnumerable<string> chars,
            IEnumerable<string> abils,
            IEnumerable<string> locs)
        {
            Characters = chars;
            Abilities = abils;
            Locations = locs;
        }
    }
}
