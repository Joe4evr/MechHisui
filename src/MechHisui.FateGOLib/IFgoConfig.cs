using System;
using System.Collections.Generic;
using System.Linq;

namespace MechHisui.FateGOLib
{
    public interface IFgoConfig
    {
        IEnumerable<ServantProfile> AllServants();
        IEnumerable<ServantProfile> FindServants(string name);
        ServantProfile GetServant(int id);
        bool AddServantAlias(string servant, string alias);

        IEnumerable<CEProfile> AllCEs();
        IEnumerable<CEProfile> FindCEs(string name);
        CEProfile GetCE(int id);
        bool AddCEAlias(string ce, string alias);

        IEnumerable<MysticCode> AllMystics();
        IEnumerable<MysticCode> FindMystics(string name);
        MysticCode GetMystic(int id);
        bool AddMysticAlias(string mystic, string alias);

        IEnumerable<FgoEvent> AllEvents();
        IEnumerable<FgoEvent> GetCurrentEvents();
    }

    //public sealed class FgoConfig
    //{
    //    public Func<IEnumerable<CEAlias>> GetCEAliases { get; set; }
    //    public Func<IEnumerable<MysticAlias>> GetMysticAliases { get; set; }
    //}
}
