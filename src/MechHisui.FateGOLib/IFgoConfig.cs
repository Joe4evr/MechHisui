using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MechHisui.FateGOLib
{
    public interface IFgoConfig
    {
        Task<IEnumerable<IServantProfile>> GetAllServantsAsync();
        Task<IEnumerable<string>> SearchServantsAsync(ServantFilterOptions options);
        Task<IEnumerable<IServantProfile>> FindServantsAsync(string name);
        Task<IServantProfile> GetServantAsync(int id);
        Task<bool> AddServantAliasAsync(string servant, string alias);

        Task<IEnumerable<ICEProfile>> GetAllCEsAsync();
        //Task<IEnumerable<string>> SearchCEsAsync(CEFilterOptions options);
        Task<IEnumerable<ICEProfile>> FindCEsByEffectAsync(string effect);
        Task<IEnumerable<ICEProfile>> FindCEsAsync(string name);
        Task<ICEProfile> GetCEAsync(int id);
        Task<bool> AddCEAliasAsync(string ce, string alias);

        Task<IEnumerable<IMysticCode>> GetAllMysticsAsync();
        Task<IEnumerable<IMysticCode>> FindMysticsAsync(string name);
        Task<IMysticCode> GetMysticAsync(int id);
        Task<bool> AddMysticAliasAsync(string mystic, string alias);

        Task<IEnumerable<IFgoEvent>> GetAllEventsAsync();
        Task<IEnumerable<IFgoEvent>> GetCurrentEventsAsync();
        Task<IEnumerable<IFgoEvent>> GetFutureEventsAsync();
        Task<IFgoEvent> AddEventAsync(string name, DateTimeOffset? start = null, DateTimeOffset? end = null, string info = null);
    }
}
