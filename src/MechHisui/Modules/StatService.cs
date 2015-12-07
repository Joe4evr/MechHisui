using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Script.v1;
using Google.Apis.Script.v1.Data;
using Google.Apis.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using Google.Apis.Util.Store;

namespace MechHisui.Modules
{
    public class StatService
    {
        public string[] Scopes = new string[] { "https://www.googleapis.com/auth/spreadsheets.readonly" };

        private List<ServantProfile> _servantProfiles = new List<ServantProfile>();
        private readonly UserCredential credential;

        public StatService(IConfiguration config)
        {
            using (Stream sr = new FileStream(Path.Combine(config["Secrets_Path"], "client_secret.json"), FileMode.Open, FileAccess.Read))
            {
                string credpath = Path.Combine(config["Secrets_Path"], "scriptcreds");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(sr).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credpath, fullPath: true)).Result;
            }

            ReadAliasList(config);
            UpdateProfileList(config);
        }

        public ServantProfile LookupStats(string servant)
        {
            var serv = servantDict.SingleOrDefault(k => k.Alias.Contains(servant.ToLowerInvariant()));

            return (serv != null) ? 
                _servantProfiles.SingleOrDefault(p => p.Name == serv.Servant) :
                _servantProfiles.SingleOrDefault(p => p.Name == servant);
        }

        public string LookupServantName(string servant)
        {
            var serv = servantDict.SingleOrDefault(k => k.Alias.Contains(servant.ToLowerInvariant()));

            return (serv != null) ?
                serv.Servant :
                servantDict.SingleOrDefault(p => p.Servant.ToLowerInvariant() == servant.ToLowerInvariant()).Servant;
        }

        //get table data and serialize to _servantProfiles so that it's cached
        public void UpdateProfileList(IConfiguration config)
        {
            string scriptId = config["Project_Key"];
            var service = new ScriptService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "MechHisui"
            });

            ExecutionRequest request = new ExecutionRequest()
            {
                Function = "exportSheet"
            };

            ScriptsResource.RunRequest runReq = service.Scripts.Run(request, scriptId);

            try
            {
                // Make the API request.
                Operation op = runReq.Execute();

                if (op.Error != null)
                {
                    // The API executed, but the script returned an error.

                    // Extract the first (and only) set of error details
                    // as a IDictionary. The values of this dictionary are
                    // the script's 'errorMessage' and 'errorType', and an
                    // array of stack trace elements. Casting the array as
                    // a JSON JArray allows the trace elements to be accessed
                    // directly.
                    IDictionary<string, object> error = op.Error.Details[0];
                    Console.WriteLine($"Script error message: {error["errorMessage"]}");
                    if (error["scriptStackTraceElements"] != null)
                    {
                        // There may not be a stacktrace if the script didn't
                        // start executing.
                        Console.WriteLine("Script error stacktrace:");
                        JArray st = (JArray)error["scriptStackTraceElements"];
                        foreach (var trace in st)
                        {
                            Console.WriteLine($"\t{trace["function"]}: {trace["lineNumber"]}");
                        }
                    }
                }
                else
                {
                    // The result provided by the API needs to be cast into
                    // the correct type, based upon what types the Apps
                    // Script function returns. Here, the function returns
                    // an Apps Script Object with String keys and values.
                    // It is most convenient to cast the return value as a JSON
                    // JObject (folderSet).
                    //JObject folderSet = (JObject)op.Response["result"];
                    //if (folderSet.Count == 0)
                    //{
                    //    Console.WriteLine("No folders returned!");
                    //}
                    //else
                    //{
                    //    Console.WriteLine("Folders under your root folder:");
                    //    foreach (var folder in folderSet)
                    //    {
                    //        Console.WriteLine("\t{0} ({1})", folder.Value, folder.Key);
                    //    }
                    //}

                    _servantProfiles = JsonConvert.DeserializeObject<List<ServantProfile>>((string)op.Response["result"]);
                }
            }
            catch (Google.GoogleApiException e)
            {
                Console.WriteLine($"Error calling API:\n{e}");
            }
        }

        public void ReadAliasList(IConfiguration config)
        {
            using (TextReader tr = new StreamReader(config["ServantAliasPath"]))
            {
                servantDict = JsonConvert.DeserializeObject<List<ServantAlias>>(tr.ReadToEnd()) ?? new List<ServantAlias>();
            }
        }

        internal static List<ServantAlias> servantDict = new List<ServantAlias>();
        //{ new[] { "" }, "Mordred" },
        //{ new[] { "" }, "Arjuna" },
        //{ new[] { "" }, "Brynhildr" },
        //{ new[] { "scath" }, "Scathach" },
        //{ new[] { "gil's bff" }, "Enkidu" },
        //{ new[] { "" }, "Karna" },
        //{ new[] { "deermud", "zero lancer" }, "Diarmuid" },
        //{ new[] { "broskander", "big alex" }, "Alexander the Great" },
        //{ new[] { "alice" }, "Nursery Rhyme" },
        //{ new[] { "jack" }, "Jack the Ripper" },
        //{ new[] { "" }, "Dr. Jekyll" },
        //{ new[] { "" }, "Frankenstein" },
    }

    public class ServantAlias
    {
        public List<string> Alias { get; set; }
        public string Servant { get; set; }
    }
}
