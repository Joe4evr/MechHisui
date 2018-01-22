using System;
using System.Collections.Generic;
using System.Text;
using Discord.Addons.SimplePermissions;
using MechHisui.SymphoXDULib;

namespace MechHisui.Core
{
    public sealed class XduConfig : IXduConfig
    {
        private readonly IConfigStore<MechHisuiConfig> _store;

        public XduConfig(IConfigStore<MechHisuiConfig> store)
        {
            _store = store;
        }


        public IEnumerable<XduProfile> AllGears()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<XduProfile> FindGears(string filter)
        {
            throw new NotImplementedException();
        }

        public XduProfile GetGear(int id)
        {
            throw new NotImplementedException();
        }


        public IEnumerable<Memoria> AllMemorias()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Memoria> FindMemorias(string filter)
        {
            throw new NotImplementedException();
        }

        public Memoria GetMemoria(int id)
        {
            throw new NotImplementedException();
        }


        public IEnumerable<XduSong> AllSongs()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<XduSong> FindSongs(string filter)
        {
            throw new NotImplementedException();
        }

        public XduSong GetSong(int id)
        {
            throw new NotImplementedException();
        }


        public IEnumerable<XduEvent> AllEvents()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<XduEvent> GetCurrentEvents()
        {
            throw new NotImplementedException();
        }


        //query helpers
    }
}
