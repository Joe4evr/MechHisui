using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using JiiLib.Net;

namespace MechHisui.FateGOLib
{
    public class StatService
    {
        internal readonly IJsonApiService ApiService;
        internal readonly FgoConfig Config;
        internal readonly Dictionary<string, Func<Task>> UpdateFuncs = new Dictionary<string, Func<Task>>();

        public StatService(FgoConfig config)
        {
            //if (apiService == null) throw new ArgumentNullException(nameof(apiService));
            if (config == null) throw new ArgumentNullException(nameof(config));

            if (!File.Exists(config.ServantAliasesPath)) throw new FileNotFoundException(nameof(config.ServantAliasesPath));
            if (!File.Exists(config.CEAliasesPath)) throw new FileNotFoundException(nameof(config.CEAliasesPath));
            if (!File.Exists(config.MysticAliasesPath)) throw new FileNotFoundException(nameof(config.MysticAliasesPath));
            if (!File.Exists(config.MasterNamesPath)) throw new FileNotFoundException(nameof(config.MasterNamesPath));
            if (!File.Exists(config.NameOnlyServantsPath)) throw new FileNotFoundException(nameof(config.NameOnlyServantsPath));

            ApiService = new GoogleScriptApiService(
                Path.Combine(config.GoogleCredPath, "client_secret.json"),
                Path.Combine(config.GoogleCredPath, "scriptcreds", "Google.Apis.Auth.OAuth2.Responses.TokenResponse-user"),
                applicationName: "MechHisui",
                projectKey: config.GoogleToken,
                functionName: "exportSheet",
                clientId: config.GoogleClientId,
                neededScopes: new string[]
                {
                    "https://www.googleapis.com/auth/spreadsheets",
                    "https://www.googleapis.com/auth/drive",
                    "https://spreadsheets.google.com/feeds/"
                });
            Config = config;

            ReadAliasList();
        }



        internal void RegisterUpdateFunc(string key, Func<Task> func)
        {
            if (!UpdateFuncs.ContainsKey(key))
                UpdateFuncs[key] = func;
        }

        public IEnumerable<ServantProfile> LookupStats(string servant, bool fullsearch = false)
        {
            var list = FgoHelpers.ServantProfiles.Concat(FgoHelpers.FakeServantProfiles);
            var servants = list
                .Where(p => p.Name.Equals(servant, StringComparison.OrdinalIgnoreCase));

            if (servants.Count() == 0 || fullsearch)
            {
                servants = servants.Concat(list.Where(p => RegexMatchOneWord(p.Name, servant)));

                if (servants.Count() == 0 || fullsearch)
                {
                    var lookup = FgoHelpers.ServantDict
                        .Where(s => s.Key.Equals(servant, StringComparison.OrdinalIgnoreCase))
                        .Select(s => s.Value)
                        .ToList();

                    if (lookup.Count == 0 || fullsearch)
                    {
                        lookup = lookup.Concat(FgoHelpers.ServantDict
                            .Where(s => RegexMatchOneWord(s.Key, servant))
                            .Select(s => s.Value))
                            .ToList();
                    }

                    if (lookup.Count > 0)
                    {
                        servants = servants.Concat(list.Where(p => lookup.Contains(p.Name)));
                    }
                }
            }

            return servants;
        }

        public IEnumerable<CEProfile> LookupCE(string name, bool fullsearch = false)
        {
            var ces = FgoHelpers.CEProfiles
                .Where(ce => ce.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (ces.Count() == 0 || fullsearch)
            {
                ces = ces.Concat(FgoHelpers.CEProfiles.Where(ce => RegexMatchOneWord(ce.Name, name)));

                if (ces.Count() == 0 || fullsearch)
                {
                    var lookup = FgoHelpers.CEDict
                        .Where(ce => ce.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                        .Select(ce => ce.Value)
                        .ToList();

                    if (lookup.Count == 0 || fullsearch)
                    {
                        lookup = lookup.Concat(FgoHelpers.CEDict.Where(ce => RegexMatchOneWord(ce.Key, name))
                            .Select(ce => ce.Value))
                            .ToList();
                    }

                    if (lookup.Count > 0)
                    {
                        ces = ces.Concat(FgoHelpers.CEProfiles.Where(ce => lookup.Contains(ce.Name)));
                    }
                }
            }

            return ces;
        }

        public IEnumerable<MysticCode> LookupMystic(string code, bool fullsearch = false)
        {
            var mystics = FgoHelpers.MysticCodeList
                .Where(m => m.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

            if (mystics.Count() == 0 || fullsearch)
            {
                mystics = mystics.Concat(FgoHelpers.MysticCodeList.Where(m => RegexMatchOneWord(m.Code, code)));

                if (mystics.Count() == 0 || fullsearch)
                {
                    var lookup = FgoHelpers.MysticCodeDict
                        .Where(m => m.Key.Equals(code, StringComparison.OrdinalIgnoreCase))
                        .Select(m => m.Value)
                        .ToList();

                    if (lookup.Count == 0 || fullsearch)
                    {
                        lookup = lookup.Concat(FgoHelpers.MysticCodeDict
                            .Where(m => RegexMatchOneWord(m.Key, code))
                            .Select(m => m.Value))
                            .ToList();
                    }

                    if (lookup.Count > 0)
                    {
                        mystics = mystics.Concat(FgoHelpers.MysticCodeList.Where(m => lookup.Contains(m.Code)));
                    }
                }
            }

            return mystics;
        }

        //public async Task UpdateEventListAsync()
        //{
        //    Console.WriteLine("Updating event list...");
        //    FgoHelpers.EventList = JsonConvert.DeserializeObject<List<Event>>(await ApiService.GetDataFromServiceAsJsonAsync("Events"));
        //}

        //public async Task UpdateMysticCodesListAsync()
        //{
        //    Console.WriteLine("Updating Mystic Codes list...");
        //    FgoHelpers.MysticCodeList = JsonConvert.DeserializeObject<List<MysticCode>>(await ApiService.GetDataFromServiceAsJsonAsync("MysticCodes"));
        //}

        //public async Task UpdateDropsListAsync()
        //{
        //    Console.WriteLine("Updating Item Drops list...");
        //    FgoHelpers.ItemDropsList = JsonConvert.DeserializeObject<List<NodeDrop>>(await ApiService.GetDataFromServiceAsJsonAsync("Drops"));
        //}

        public void ReadAliasList()
        {
            FgoHelpers.ServantDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Config.ServantAliasesPath));
            FgoHelpers.CEDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Config.CEAliasesPath));
            FgoHelpers.MysticCodeDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Config.MysticAliasesPath));
        }

        private static bool RegexMatchOneWord(string hay, string needle)
            => Regex.Match(hay, String.Concat(b, needle, b), RegexOptions.IgnoreCase).Success;

        private const string b = @"\b";

        //{ new[] [ "gil's bff" ], "Enkidu" },
    }
}
