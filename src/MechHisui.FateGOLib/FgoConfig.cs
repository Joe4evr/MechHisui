using System;
using System.Collections.Generic;
using System.Linq;

namespace MechHisui.FateGOLib
{
    public sealed class FgoConfig
    {
        public Func<string, IEnumerable<ServantProfile>> FindServants { get; set; } = term => Enumerable.Empty<ServantProfile>();
        //public Func<IEnumerable<ServantProfile>> GetFakeServants { get; set; }
        //public Func<IEnumerable<ServantAlias>> GetServantAliases { get; set; }
        public Func<string, string, bool> AddServantAlias { get; set; } = (name, alias) => false;

        public Func<IEnumerable<CEProfile>> GetCEs { get; set; } = Enumerable.Empty<CEProfile>;
        public Func<IEnumerable<CEAlias>> GetCEAliases { get; set; }
        public Func<string, string, bool> AddCEAlias { get; set; } = (ce, alias) => false;

        public Func<IEnumerable<MysticCode>> GetMystics { get; set; } = Enumerable.Empty<MysticCode>;
        public Func<IEnumerable<MysticAlias>> GetMysticAliases { get; set; }
        public Func<string, string, bool> AddMysticAlias { get; set; } = (code, alias) => false;

        public Func<IEnumerable<FgoEvent>> GetEvents { get; set; } = Enumerable.Empty<FgoEvent>;
    }
}
