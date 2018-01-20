using System;
using System.Collections.Generic;
using System.Linq;

namespace MechHisui.FateGOLib
{
    public interface IFgoConfig
    {
        IEnumerable<IServantProfile> AllServants();
        IEnumerable<IServantProfile> FindServants(string name);
        IServantProfile GetServant(int id);
        bool AddServantAlias(string servant, string alias);

        IEnumerable<ICEProfile> AllCEs();
        IEnumerable<ICEProfile> FindCEs(string name);
        ICEProfile GetCE(int id);
        bool AddCEAlias(string ce, string alias);

        IEnumerable<IMysticCode> AllMystics();
        IEnumerable<IMysticCode> FindMystics(string name);
        IMysticCode GetMystic(int id);
        bool AddMysticAlias(string mystic, string alias);

        IEnumerable<IFgoEvent> AllEvents();
        IEnumerable<IFgoEvent> GetCurrentEvents();
    }

    //public sealed class FgoConfig
    //{
    //    public Func<IEnumerable<CEAlias>> GetCEAliases { get; set; }
    //    public Func<IEnumerable<MysticAlias>> GetMysticAliases { get; set; }
    //}
}
