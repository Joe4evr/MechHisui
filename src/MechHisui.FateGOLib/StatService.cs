using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JiiLib;
using JiiLib.Net;
using Newtonsoft.Json;

namespace MechHisui.FateGOLib
{
    public class StatService
    {
        private readonly GoogleScriptApiService _apiService;
        private readonly string _servantAliasPath;
        private readonly string _ceAliasPath;
        private readonly string _mysticAliasPath;

        public StatService(GoogleScriptApiService apiService, string servantAliasPath, string ceAliasPath, string mysticAliasPath)
        {
            if (apiService == null) throw new ArgumentNullException(nameof(apiService));
            if (servantAliasPath == null) throw new ArgumentNullException(nameof(servantAliasPath));
            if (ceAliasPath == null) throw new ArgumentNullException(nameof(ceAliasPath));
            if (mysticAliasPath == null) throw new ArgumentNullException(nameof(mysticAliasPath));

            if (!File.Exists(servantAliasPath)) throw new FileNotFoundException(nameof(servantAliasPath));
            if (!File.Exists(ceAliasPath)) throw new FileNotFoundException(nameof(ceAliasPath));
            if (!File.Exists(mysticAliasPath)) throw new FileNotFoundException(nameof(mysticAliasPath));

            _apiService = apiService;
            _servantAliasPath = servantAliasPath;
            _ceAliasPath = ceAliasPath;
            _mysticAliasPath = mysticAliasPath;

            ReadAliasList();
        }

        public string LookupServantName(string servant)
        {
            var serv = FgoHelpers.ServantDict.SingleOrDefault(k => k.Alias.ContainsIgnoreCase(servant));

            return (serv != null) ?
                serv.Servant :
                FgoHelpers.ServantDict.SingleOrDefault(p => p.Servant.ToLowerInvariant() == servant.ToLowerInvariant())?.Servant;
        }

        public ServantProfile LookupStats(string servant)
        {
            var serv = FgoHelpers.ServantDict.SingleOrDefault(k => k.Alias.ContainsIgnoreCase(servant));
            if (serv != null)
            {
                Func<ServantProfile, bool> pred = p => p.Name == serv.Servant;
                return FgoHelpers.ServantProfiles.SingleOrDefault(pred)
                     ?? FgoHelpers.FakeServantProfiles.SingleOrDefault(pred);
            }
            else
            {
                return FgoHelpers.ServantProfiles.SingleOrDefault(p => p.Name.ToLowerInvariant() == servant.ToLowerInvariant())
                    ?? FgoHelpers.ServantProfiles.SingleOrDefault(p => p.Name.ContainsIgnoreCase(servant));
            }
        }

        public CEProfile LookupCE(string name)
        {
            var ce = FgoHelpers.CEDict.SingleOrDefault(k => k.Alias.ContainsIgnoreCase(name));
            if (ce != null)
            {
                return FgoHelpers.CEProfiles.SingleOrDefault(p => p.Name == ce.CE);
            }
            else
            {
                return FgoHelpers.CEProfiles.SingleOrDefault(p => p.Name.ToLowerInvariant() == name.ToLowerInvariant())
                    ?? FgoHelpers.CEProfiles.SingleOrDefault(p => p.Name.ContainsIgnoreCase(name.ToLowerInvariant()));
            }
        }

        public MysticCode LookupMystic(string code)
        {
            var mystic = FgoHelpers.MysticCodeDict.SingleOrDefault(m => m.Alias.ContainsIgnoreCase(code));
            if (mystic != null)
            {
                return FgoHelpers.MysticCodeList.SingleOrDefault(m => m.Code == mystic.Code);
            }
            else
            {
                return FgoHelpers.MysticCodeList.SingleOrDefault(m => m.Code.ToLowerInvariant() == code.ToLowerInvariant());
            }
        }

        //get table data and serialize to the respective lists so that they're cached
        public async Task UpdateProfileListsAsync()
        {
            Console.WriteLine("Updating profile lists...");
            _apiService.Parameters = new List<object> { "Servants" };
            FgoHelpers.ServantProfiles = JsonConvert.DeserializeObject<List<ServantProfile>>(await _apiService.GetDataFromServiceAsJsonAsync(), new FgoProfileConverter());
            _apiService.Parameters = new List<object> { "FakeServants" };
            FgoHelpers.FakeServantProfiles = JsonConvert.DeserializeObject<List<ServantProfile>>(await _apiService.GetDataFromServiceAsJsonAsync(), new FgoProfileConverter());
        }

        public async Task UpdateCEListAsync()
        {
            Console.WriteLine("Updating CE list...");
            _apiService.Parameters = new List<object> { "CEs" };
            FgoHelpers.CEProfiles = JsonConvert.DeserializeObject<List<CEProfile>>(await _apiService.GetDataFromServiceAsJsonAsync());
        }

        public async Task UpdateEventListAsync()
        {
            Console.WriteLine("Updating event list...");
            _apiService.Parameters = new List<object> { "Events" };
            FgoHelpers.EventList = JsonConvert.DeserializeObject<List<Event>>(await _apiService.GetDataFromServiceAsJsonAsync());
        }

        public async Task UpdateMysticCodesListAsync()
        {
            Console.WriteLine("Updating Mystic Codes list...");
            _apiService.Parameters = new List<object> { "MysticCodes" };
            FgoHelpers.MysticCodeList = JsonConvert.DeserializeObject<List<MysticCode>>(await _apiService.GetDataFromServiceAsJsonAsync());
        }

        public async Task UpdateDropsListAsync()
        {
            Console.WriteLine("Updating Item Drops list...");
            _apiService.Parameters = new List<object> { "Drops" };
            FgoHelpers.ItemDropsList = JsonConvert.DeserializeObject<List<NodeDrop>>(await _apiService.GetDataFromServiceAsJsonAsync());
        }

        public void ReadAliasList()
        {
            using (TextReader tr = new StreamReader(_servantAliasPath))
            {
                FgoHelpers.ServantDict = JsonConvert.DeserializeObject<List<ServantAlias>>(tr.ReadToEnd()) ?? new List<ServantAlias>();
            }
            using (TextReader tr = new StreamReader(_ceAliasPath))
            {
                FgoHelpers.CEDict = JsonConvert.DeserializeObject<List<CEAlias>>(tr.ReadToEnd()) ?? new List<CEAlias>();
            }
            using (TextReader tr = new StreamReader(_mysticAliasPath))
            {
                FgoHelpers.MysticCodeDict = JsonConvert.DeserializeObject<List<MysticAlias>>(tr.ReadToEnd()) ?? new List<MysticAlias>();
            }
        }
        
        //{ new[] [ "" ], "Brynhildr" },
        //{ new[] [ "gil's bff" ], "Enkidu" },
        //{ new[] [ "broskander", "big alex" ], "Alexander the Great" },
    }
}
