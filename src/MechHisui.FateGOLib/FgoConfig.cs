using System;
using System.Collections.Generic;

namespace MechHisui.FateGOLib
{
    public sealed class FgoConfig
    {
        public Func<IEnumerable<ServantProfile>> GetServants { get; set; }
        public Func<IEnumerable<ServantProfile>> GetFakeServants { get; set; }
        //public Func<IEnumerable<ServantAlias>> GetServantAliases { get; set; }
        public Func<string, string, bool> AddServantAlias { get; set; }

        public Func<IEnumerable<CEProfile>> GetCEs { get; set; }
        //public Func<IEnumerable<CEAlias>> GetCEAliases { get; set; }
        public Func<string, string, bool> AddCEAlias { get; set; }

        public Func<IEnumerable<MysticCode>> GetMystics { get; set; }
        //public Func<IEnumerable<MysticAlias>> GetMysticAliases { get; set; }
        public Func<string, string, bool> AddMysticAlias { get; set; }

        public Func<IEnumerable<Event>> GetEvents { get; set; }

        //public string GoogleClientId { get; set; }

        //public string GoogleToken { get; set; }

        //public string GoogleCredPath { get; set; }

        //public string ServantAliasesPath { get; set; }

        //public string CEAliasesPath { get; set; }

        //public string MysticAliasesPath { get; set; }

        //public string MasterNamesPath { get; set; }

        //public string NameOnlyServantsPath { get; set; }
    }
}
