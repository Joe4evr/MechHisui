using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        public IEnumerable<ServantProfile> LookupStats(string servant, bool fullsearch = false)
        {
            var servants = FgoHelpers.ServantProfiles.Concat(FgoHelpers.FakeServantProfiles)
                .Where(p => p.Name.Equals(servant, StringComparison.InvariantCultureIgnoreCase));

            if (servants.Count() == 0 || fullsearch)
            {
                servants = FgoHelpers.ServantProfiles.Concat(FgoHelpers.FakeServantProfiles)
                    .Where(p => RegexMatchOneWord(p.Name, servant));

                if (servants.Count() == 0 || fullsearch)
                {
                    var lookup = FgoHelpers.ServantDict
                        .Where(s => s.Key.Equals(servant, StringComparison.InvariantCultureIgnoreCase))
                        .Select(s => s.Value)
                        .ToList();

                    if (lookup.Count == 0 || fullsearch)
                    {
                        lookup = FgoHelpers.ServantDict
                            .Where(s => RegexMatchOneWord(s.Key, servant))
                            .Select(s => s.Value)
                            .ToList();
                    }

                    if (lookup.Count > 0)
                    {
                        servants = FgoHelpers.ServantProfiles.Concat(FgoHelpers.FakeServantProfiles)
                            .Where(p => lookup.Contains(p.Name));
                    }
                }
            }

            return servants;
        }

        public IEnumerable<CEProfile> LookupCE(string name, bool fullsearch = false)
        {
            var ces = FgoHelpers.CEProfiles.Where(ce => ce.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

            if (ces.Count() == 0 || fullsearch)
            {
                ces = FgoHelpers.CEProfiles.Where(ce => RegexMatchOneWord(ce.Name, name));

                if (ces.Count() == 0 || fullsearch)
                {
                    var lookup = FgoHelpers.CEDict
                        .Where(ce => ce.Key.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                        .Select(ce => ce.Value)
                        .ToList();

                    if (lookup.Count == 0 || fullsearch)
                    {
                        lookup = FgoHelpers.CEDict.Where(ce => RegexMatchOneWord(ce.Key, name))
                            .Select(ce => ce.Value)
                            .ToList();
                    }

                    if (lookup.Count > 0)
                    {
                        ces = FgoHelpers.CEProfiles.Where(ce => lookup.Contains(ce.Name));
                    }
                }
            }

            return ces;
        }

        public IEnumerable<MysticCode> LookupMystic(string code, bool fullsearch = false)
        {
            var mystics = FgoHelpers.MysticCodeList.Where(m => m.Code.Equals(code, StringComparison.InvariantCultureIgnoreCase));

            if (mystics.Count() == 0 || fullsearch)
            {
                mystics = FgoHelpers.MysticCodeList.Where(m => RegexMatchOneWord(m.Code, code));
            }

            if (mystics.Count() == 0 || fullsearch)
            {
                var lookup = FgoHelpers.MysticCodeDict
                    .Where(m => m.Key.Equals(code, StringComparison.InvariantCultureIgnoreCase))
                    .Select(m => m.Value)
                    .ToList();

                if (lookup.Count == 0 || fullsearch)
                {
                    lookup = FgoHelpers.MysticCodeDict
                        .Where(m => RegexMatchOneWord(m.Key, code))
                        .Select(m => m.Value)
                        .ToList();
                }

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
            FgoHelpers.ServantDict    = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(_servantAliasPath));
            FgoHelpers.CEDict         = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(_ceAliasPath));
            FgoHelpers.MysticCodeDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(_mysticAliasPath));
        }

        private static bool RegexMatchOneWord(string hay, string needle)
            => Regex.Match(hay, String.Concat(b, needle, b), RegexOptions.IgnoreCase).Success;

        private const string b = @"\b";

        //{ new[] [ "gil's bff" ], "Enkidu" },
    }
}
