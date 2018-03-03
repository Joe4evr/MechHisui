using System;
using System.Collections.Generic;

namespace MechHisui.SymphoXDULib
{
    public interface IXduConfig
    {
        IEnumerable<IXduProfile> AllGears();
        IEnumerable<IXduProfile> FindGears(string filter);
        IXduProfile GetGear(int id);

        IEnumerable<IMemoria> AllMemorias();
        IEnumerable<IMemoria> FindMemorias(string filter);
        IMemoria GetMemoria(int id);

        IEnumerable<IXduSong> AllSongs();
        IEnumerable<IXduSong> FindSongs(string filter);
        IXduSong GetSong(int id);

        IEnumerable<IXduEvent> AllEvents();
        IEnumerable<IXduEvent> GetCurrentEvents();
    }

    //public class XduConfig
    //{
    //    public Func<IEnumerable<XduProfile>> GetGears { get; set; } = Enumerable.Empty<XduProfile>;
    //    public Func<IEnumerable<Memoria>> GetMemorias { get; set; } = Enumerable.Empty<Memoria>;
    //    public Func<IEnumerable<XduSong>> GetSongs { get; set; } = Enumerable.Empty<XduSong>;
    //    public Func<IEnumerable<XduEvent>> GetEvents { get; set; } = Enumerable.Empty<XduEvent>;
    //}
}
