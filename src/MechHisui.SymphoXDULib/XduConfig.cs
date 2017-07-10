using System;
using System.Collections.Generic;
using System.Text;

namespace MechHisui.SymphoXDULib
{
    public class XduConfig
    {
        public Func<IEnumerable<Profile>> GetGears { get; set; }
        public Func<IEnumerable<Memoria>> GetMemorias { get; set; }
        public Func<IEnumerable<Song>> GetSongs { get; set; }
        public Func<IEnumerable<XduEvent>> GetEvents { get; set; }
    }
}
