using System;
using System.Collections.Generic;
using Discord.Addons.SimplePermissions;
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


        public IEnumerable<IServantProfile> AllServants()
        {
            throw new NotImplementedException();
        }

        public IServantProfile GetServant(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IServantProfile> FindServants(string name)
        {
            throw new NotImplementedException();
        }

        public bool AddServantAlias(string servant, string alias)
        {
            throw new NotImplementedException();
        }


        public IEnumerable<ICEProfile> AllCEs()
        {
            throw new NotImplementedException();
        }

        public ICEProfile GetCE(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ICEProfile> FindCEs(string name)
        {
            throw new NotImplementedException();
        }

        public bool AddCEAlias(string ce, string alias)
        {
            throw new NotImplementedException();
        }


        public IEnumerable<IMysticCode> AllMystics()
        {
            throw new NotImplementedException();
        }

        public IMysticCode GetMystic(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IMysticCode> FindMystics(string name)
        {
            throw new NotImplementedException();
        }

        public bool AddMysticAlias(string mystic, string alias)
        {
            throw new NotImplementedException();
        }


        public IEnumerable<IFgoEvent> AllEvents()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IFgoEvent> GetCurrentEvents()
        {
            throw new NotImplementedException();
        }
    }
}
