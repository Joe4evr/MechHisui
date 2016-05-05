using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        public IEnumerable<ServantProfile> LookupStats(string servant)
        {
            var servants = FgoHelpers.ServantProfiles.Where(p => p.Name.ContainsIgnoreCase(servant))
                .Concat(FgoHelpers.FakeServantProfiles.Where(p => p.Name.ContainsIgnoreCase(servant)));

            if (servants.Count() == 0)
            {
                var lookup = FgoHelpers.ServantDict.Where(k => k.Alias.Any(a => RegexMatch(a, servant)))
                    .Select(l => l.Servant)
                    .ToList();
                if (lookup.Count > 0)
                {
                    Func<ServantProfile, bool> pred = p => lookup.Contains(p.Name);

                    servants = FgoHelpers.ServantProfiles.Where(pred)
                        .Concat(FgoHelpers.FakeServantProfiles.Where(pred));
                }
            }

            return servants;
        }

        public IEnumerable<CEProfile> LookupCE(string name)
        {
            var ces = FgoHelpers.CEProfiles.Where(ce => ce.Name.ContainsIgnoreCase(name));

            if (ces.Count() == 0)
            {
                var lookup = FgoHelpers.CEDict.Where(k => k.Alias.Any(a => RegexMatch(a, name)))
                    .Select(l => l.CE)
                    .ToList();
                if (lookup.Count > 0)
                {
                    ces = FgoHelpers.CEProfiles.Where(ce => lookup.Contains(ce.Name));
                }
            }

            return ces;
        }

        public IEnumerable<MysticCode> LookupMystic(string code)
        {
            var mystics = FgoHelpers.MysticCodeList.Where(m => m.Code.ContainsIgnoreCase(code));

            if (mystics.Count() == 0)
            {
                var lookup = FgoHelpers.MysticCodeDict.Where(m => m.Alias.Any(a => RegexMatch(a, code)))
                    .Select(l => l.Code)
                    .ToList();
                if (lookup.Count > 0)
                {
                    mystics = FgoHelpers.MysticCodeList.Where(m => lookup.Contains(m.Code));
                }
            }

            return mystics;
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

        private static readonly Func<string, string, bool> RegexMatch = (alias, str) =>
            Regex.Match(alias, String.Concat(b, str, b), RegexOptions.IgnoreCase).Success;

        private const string b = @"\b";

        //{ new[] [ "" ], "Brynhildr" },
        //{ new[] [ "gil's bff" ], "Enkidu" },
        //{ new[] [ "broskander", "big alex" ], "Alexander the Great" },
    }
}
