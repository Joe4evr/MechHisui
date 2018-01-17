using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Discord.Addons.SimplePermissions;
using Discord.WebSocket;
using MechHisui.FateGOLib;

namespace MechHisui.Core
{
    public sealed class FgoConfig : IFgoConfig
    {
        private readonly IConfigStore<MechHisuiConfig> _store;

        public FgoConfig(IConfigStore<MechHisuiConfig> store)
        {
            _store = store;
        }


        public IEnumerable<ServantProfile> AllServants()
        {
            throw new NotImplementedException();
        }

        public ServantProfile GetServant(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ServantProfile> FindServants(string name)
        {
            throw new NotImplementedException();
        }

        public bool AddServantAlias(string servant, string alias)
        {
            throw new NotImplementedException();
        }


        public IEnumerable<CEProfile> AllCEs()
        {
            throw new NotImplementedException();
        }

        public CEProfile GetCE(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<CEProfile> FindCEs(string name)
        {
            throw new NotImplementedException();
        }

        public bool AddCEAlias(string ce, string alias)
        {
            throw new NotImplementedException();
        }


        public IEnumerable<MysticCode> AllMystics()
        {
            throw new NotImplementedException();
        }

        public MysticCode GetMystic(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<MysticCode> FindMystics(string name)
        {
            throw new NotImplementedException();
        }

        public bool AddMysticAlias(string mystic, string alias)
        {
            throw new NotImplementedException();
        }


        public IEnumerable<FgoEvent> AllEvents()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<FgoEvent> GetCurrentEvents()
        {
            throw new NotImplementedException();
        }
    }
}
