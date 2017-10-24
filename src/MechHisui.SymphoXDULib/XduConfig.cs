using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MechHisui.SymphoXDULib
{
    public class XduConfig
    {
        public Func<IEnumerable<XduProfile>> GetGears { get; set; } = Enumerable.Empty<XduProfile>;
        public Func<IEnumerable<Memoria>> GetMemorias { get; set; } = Enumerable.Empty<Memoria>;
        public Func<IEnumerable<XduSong>> GetSongs { get; set; } = Enumerable.Empty<XduSong>;
        public Func<IEnumerable<XduEvent>> GetEvents { get; set; } = Enumerable.Empty<XduEvent>;
    }
}
