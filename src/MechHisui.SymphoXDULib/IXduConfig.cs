using System;
using System.Collections.Generic;

namespace MechHisui.SymphoXDULib
{
    public interface IXduConfig
    {
        IEnumerable<XduProfile> AllGears();
        IEnumerable<XduProfile> FindGears(string filter);
        XduProfile GetGear(int id);

        IEnumerable<Memoria> AllMemorias();
        IEnumerable<Memoria> FindMemorias(string filter);
        Memoria GetMemoria(int id);

        IEnumerable<XduSong> AllSongs();
        IEnumerable<XduSong> FindSongs(string filter);
        XduSong GetSong(int id);

        IEnumerable<XduEvent> AllEvents();
        IEnumerable<XduEvent> GetCurrentEvents();
    }

    //public class XduConfig
    //{
    //    public Func<IEnumerable<XduProfile>> GetGears { get; set; } = Enumerable.Empty<XduProfile>;
    //    public Func<IEnumerable<Memoria>> GetMemorias { get; set; } = Enumerable.Empty<Memoria>;
    //    public Func<IEnumerable<XduSong>> GetSongs { get; set; } = Enumerable.Empty<XduSong>;
    //    public Func<IEnumerable<XduEvent>> GetEvents { get; set; } = Enumerable.Empty<XduEvent>;
    //}
}
