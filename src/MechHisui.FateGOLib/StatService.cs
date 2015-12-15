using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JiiLib.Net;
using Newtonsoft.Json;

namespace MechHisui.FateGOLib
{
    public class StatService
    {
        private readonly GoogleScriptApiService _apiService;
        private readonly string _aliasPath;
        private readonly string _ceAliasPath;

        public StatService(GoogleScriptApiService apiService, string aliasPath, string ceAliasPath)
        {
            if (apiService == null) throw new ArgumentNullException(nameof(apiService));
            if (aliasPath == null) throw new ArgumentNullException(nameof(aliasPath));
            if (ceAliasPath == null) throw new ArgumentNullException(nameof(ceAliasPath));

            if (!File.Exists(aliasPath)) throw new FileNotFoundException(nameof(aliasPath));
            if (!File.Exists(ceAliasPath)) throw new FileNotFoundException(nameof(ceAliasPath));

            _apiService = apiService;
            _aliasPath = aliasPath;
            _ceAliasPath = ceAliasPath;

            ReadAliasList();
        }

        public ServantProfile LookupStats(string servant)
        {
            var serv = FgoHelpers.ServantDict.SingleOrDefault(k => k.Alias.Contains(servant.ToLowerInvariant()));
            if (serv != null)
            {
                Func<ServantProfile, bool> pred = p => p.Name == serv.Servant;
                return FgoHelpers.ServantProfiles.SingleOrDefault(pred) ??
                       FgoHelpers.FakeServantProfiles.SingleOrDefault(pred);
            }
            else
            {
                return FgoHelpers.ServantProfiles.SingleOrDefault(p => p.Name.ToLowerInvariant() == servant.ToLowerInvariant());
            }
        }

        public CEProfile LookupCE(string name)
        {
            var ce = FgoHelpers.CEDict.SingleOrDefault(k => k.Alias.Contains(name.ToLowerInvariant()));
            if (ce != null)
            {
                return FgoHelpers.CEProfiles.SingleOrDefault(p => p.Name == ce.CE);
            }
            else
            {
                return FgoHelpers.CEProfiles.SingleOrDefault(p => p.Name.ToLowerInvariant() == name.ToLowerInvariant());
            }
        }

        public string LookupServantName(string servant)
        {
            var serv = FgoHelpers.ServantDict.SingleOrDefault(k => k.Alias.Contains(servant.ToLowerInvariant()));

            return (serv != null) ?
                serv.Servant :
                FgoHelpers.ServantDict.SingleOrDefault(p => p.Servant.ToLowerInvariant() == servant.ToLowerInvariant())?.Servant;
        }

        //get table data and serialize to _servantProfiles so that it's cached
        public async Task UpdateProfileListsAsync()
        {
            _apiService.Parameters = new List<object> { "Servants" };
            FgoHelpers.ServantProfiles = JsonConvert.DeserializeObject<List<ServantProfile>>(await _apiService.GetDataFromServiceAsJsonAsync());
            _apiService.Parameters = new List<object> { "FakeServants" };
            FgoHelpers.FakeServantProfiles = JsonConvert.DeserializeObject<List<ServantProfile>>(await _apiService.GetDataFromServiceAsJsonAsync());
            _apiService.Parameters = new List<object> { "CEs" };
            FgoHelpers.CEProfiles = JsonConvert.DeserializeObject<List<CEProfile>>(await _apiService.GetDataFromServiceAsJsonAsync());
        }

        public async Task UpdateEventListsAsync()
        {
            _apiService.Parameters = new List<object> { "Events" };
            FgoHelpers.EventList = JsonConvert.DeserializeObject<List<Event>>(await _apiService.GetDataFromServiceAsJsonAsync());
        }

        //string scriptId = config["Project_Key"];
        //var service = new ScriptService(new BaseClientService.Initializer()
        //{
        //    HttpClientInitializer = credential,
        //    ApplicationName = "MechHisui"
        //});

        //ExecutionRequest request = new ExecutionRequest()
        //{
        //    Function = "exportServants"
        //};

        //ScriptsResource.RunRequest runReq = service.Scripts.Run(request, scriptId);

        //try
        //{
        //    // Make the API request.
        //    Operation op = runReq.Execute();

        //    if (op.Error != null)
        //    {
        //        // The API executed, but the script returned an error.

        //        // Extract the first (and only) set of error details
        //        // as a IDictionary. The values of this dictionary are
        //        // the script's 'errorMessage' and 'errorType', and an
        //        // array of stack trace elements. Casting the array as
        //        // a JSON JArray allows the trace elements to be accessed
        //        // directly.
        //        IDictionary<string, object> error = op.Error.Details[0];
        //        Console.WriteLine($"Script error message: {error["errorMessage"]}");
        //        if (error["scriptStackTraceElements"] != null)
        //        {
        //            // There may not be a stacktrace if the script didn't
        //            // start executing.
        //            Console.WriteLine("Script error stacktrace:");
        //            JArray st = (JArray)error["scriptStackTraceElements"];
        //            foreach (var trace in st)
        //            {
        //                Console.WriteLine($"\t{trace["function"]}: {trace["lineNumber"]}");
        //            }
        //        }
        //    }
        //    else
        //    {
        //        // The result provided by the API needs to be cast into
        //        // the correct type, based upon what types the Apps
        //        // Script function returns. Here, the function returns
        //        // an Apps Script Object with String keys and values.
        //        // It is most convenient to cast the return value as a JSON
        //        // JObject (folderSet).
        //        //JObject folderSet = (JObject)op.Response["result"];
        //        //if (folderSet.Count == 0)
        //        //{
        //        //    Console.WriteLine("No folders returned!");
        //        //}
        //        //else
        //        //{
        //        //    Console.WriteLine("Folders under your root folder:");
        //        //    foreach (var folder in folderSet)
        //        //    {
        //        //        Console.WriteLine("\t{0} ({1})", folder.Value, folder.Key);
        //        //    }
        //        //}

        //        var temp = (string)op.Response["result"];
        //        using (TextWriter tw = new StreamWriter(Path.Combine(config["Logs"], "servants.json")))
        //        {
        //            tw.Write(temp);
        //        }
        //        _servantProfiles = JsonConvert.DeserializeObject<List<ServantProfile>>(temp);
        //    }
        //}
        //catch (Google.GoogleApiException e)
        //{
        //    Console.WriteLine($"Error calling API:\n{e}");
        //}

        public void ReadAliasList()
        {
            using (TextReader tr = new StreamReader(_aliasPath))
            {
                FgoHelpers.ServantDict = JsonConvert.DeserializeObject<List<ServantAlias>>(tr.ReadToEnd()) ?? new List<ServantAlias>();
            }
            using (TextReader tr = new StreamReader(_ceAliasPath))
            {
                FgoHelpers.CEDict = JsonConvert.DeserializeObject<List<CEAlias>>(tr.ReadToEnd()) ?? new List<CEAlias>();
            }
        }

        //{ new[] { "moedred" }, "Mordred" },
        //{ new[] { "indian archer" }, "Arjuna" },
        //{ new[] { "" }, "Brynhildr" },
        //{ new[] { "gil's bff" }, "Enkidu" },
        //{ new[] { "" }, "Karna" },
        //{ new[] { "broskander", "big alex" }, "Alexander the Great" },
        //{ new[] { "light" }, "Dr. Jekyll" },
        //{ new[] { "" }, "Frankenstein" },
    }
}
