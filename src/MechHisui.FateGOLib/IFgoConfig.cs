using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JiiLib.SimpleDsl;

namespace MechHisui.FateGOLib
{
    public interface IFgoConfig
    {
        //r
        Task<IEnumerable<IServantProfile>> GetAllServantsAsync();
        Task<IEnumerable<string>> SearchServantsAsync(QueryParseResult<IServantProfile> options);
        Task<IEnumerable<IServantProfile>> FindServantsAsync(string name);
        Task<IServantProfile> GetServantAsync(int id);
        //w
        Task<bool> AddServantAliasAsync(string servant, string alias);

        //r
        Task<IEnumerable<ICEProfile>> GetAllCEsAsync();
        Task<IEnumerable<string>> SearchCEsAsync(QueryParseResult<ICEProfile> options);
        Task<IEnumerable<ICEProfile>> FindCEsByEffectAsync(string effect);
        Task<IEnumerable<ICEProfile>> FindCEsAsync(string name);
        Task<ICEProfile> GetCEAsync(int id);
        //w
        Task<bool> AddCEAliasAsync(string ce, string alias);

        //r
        Task<IEnumerable<IMysticCode>> GetAllMysticsAsync();
        Task<IEnumerable<IMysticCode>> FindMysticsAsync(string name);
        Task<IMysticCode> GetMysticAsync(int id);
        //w
        Task<bool> AddMysticAliasAsync(string mystic, string alias);

        //r
        Task<IEnumerable<IFgoEvent>> GetAllEventsAsync();
        Task<IEnumerable<IFgoEvent>> GetCurrentEventsAsync();
        Task<IEnumerable<IFgoEvent>> GetFutureEventsAsync();
        //w
        Task<IFgoEvent> AddEventAsync(string name, EventProperties eventProperties);
        Task<IFgoEvent> EditEventAsync(int id, EventProperties updatedProperties);
    }
}
