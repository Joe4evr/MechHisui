using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Script.v1;
using Google.Apis.Script.v1.Data;
using Google.Apis.Services;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace MechHisui.Modules
{
    public class Wikier
    {
        public const string BasePath = "http://fategrandorder.wikia.com";
        public const string BaseApi = BasePath + "/api/v1/";

        public string[] Scopes = new string[] { "https://www.googleapis.com/auth/drive" };

        private readonly RestClient _client;
        private readonly List<ServantProfile> _servantProfiles = new List<ServantProfile>();

        public Wikier(IConfiguration config)
        {
            _client = new RestClient(BaseApi)
            {
                
            };

            UserCredential credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets() { ClientId = config["GDriveUser"], ClientSecret = config["GDriveToken"] },
                Scopes,
                "user",
                new CancellationToken()
            ).Result;

            //TODO: get table data and serialize to _servantProfiles so that it's cached
            string scriptId = "";
            var service = new ScriptService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "MechHisui"
            });

            ExecutionRequest request = new ExecutionRequest()
            {
                Function = "getGameData"
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
                    JObject folderSet = (JObject)op.Response["result"];
                    if (folderSet.Count == 0)
                    {
                        Console.WriteLine("No folders returned!");
                    }
                    else
                    {
                        Console.WriteLine("Folders under your root folder:");
                        foreach (var folder in folderSet)
                        {
                            Console.WriteLine("\t{0} ({1})", folder.Value, folder.Key);
                        }
                    }
                }
            }
            catch (Google.GoogleApiException e)
            {
                Console.WriteLine($"Error calling API:\n{e}");
            }
        }

        public ServantProfile LookupStats(string servant)
        {
            string lookup = String.Empty;
            var key = servantDict.Keys.Where(k => k.Contains(servant.ToLowerInvariant())).SingleOrDefault();

            return (key != null && servantDict.TryGetValue(key, out lookup)) ? 
                _servantProfiles.Where(p => p.Name == lookup).SingleOrDefault() :
                _servantProfiles.Where(p => p.Name == servant).SingleOrDefault();
        }

        //private async Task<string> Send(RestRequest request, CancellationToken cancelToken)
        //{
        //    int retryCount = 0;
        //    while (true)
        //    {
        //        var response = await _client.ExecuteTaskAsync(request, cancelToken).ConfigureAwait(false);
        //        int statusCode = (int)response.StatusCode;
        //        if (statusCode == 0) //Internal Error
        //        {
        //            if (response.ErrorException.HResult == -2146233079 && retryCount++ < 5) //The request was aborted: Could not create SSL/TLS secure channel.
        //                continue; //Seems to work if we immediately retry
        //            throw response.ErrorException;
        //        }
        //        if (statusCode < 200 || statusCode >= 300) //2xx = Success
        //            throw new Exception(response.StatusCode.ToString());
        //        return response.Content;
        //    }
        //}
        
        internal static IReadOnlyDictionary<string[], string> servantDict = new Dictionary<string[], string>()
        {
            { new[] { "shielder", "mashu" },                "Mash Kyrielight" },

            //Sabers
            { new[] { "saber", "artoria", "arthuria", "king of hungry" }, "Arturia Pendragon" },
            { new[] { "saber alter" },                      "Arturia Pendragon (Alter)" },
            { new[] { "saber lily" },                       "Arturia Pendragon (Lily)" },
            { new[] { "nero", "umu", "emprah" },            "Nero Claudius Ceasar" },
            { new[] { "sieg", "literal shit" },             "Siegfried" },
            { new[] { "ceasar", "fat saber", "faber" },     "Julius Gaius Caesar" },
            { new[] { "jets" },                             "Attila" },
            { new[] { "saber gilles", "uncool gilles" },    "Gilles de Rais (Saber)" },
            { new[] { "deon", "trap saber" },               "Le Chevalier d'Eon" },
            { new[] { "okita", "sakusaber" },               "Okita Souji" },

            //Archers
            { new[] { "emiya", "garcher", "red man" },      "EMIYA" },
            { new[] { "gil", "gilgil", "goldilocks" },      "Gilgamesh" },
            { new[] { "robin" },                            "Robin Hood" },
            { new[] { "nyanta", "atanyanta", "evil cat" },  "Atalanta" },
            { new[] { "gorgon archer" },                    "Euryale" },
            { new[] { "atrash", "aloha snackbar" },         "Arash" },
            { new[] { "artemis", "tittymonster" },          "Orion"},
            //{ new[] { "" },                               "David" },
            { new[] { "nobu", "nobunaga" },                 "Oda Nobunaga" },

            //Lancers
            { new[] { "cu", "dog", "blue man" },            "Cu Chulain (Lancer)" },
            { new[] { "liz", "eli", "cutest" },             "Elizabeth Bathory" },
            { new[] { "benkei" },                           "Musashibo Benkei" },
            { new[] { "proto cu", "proto dog" },            "Cu Chulain (Proto)" },
            { new[] { "leo" },                              "Leonidas I" },
            { new[] { "roma" },                             "Romulus" },
            //{ new[] { "" },                               "Hector" },
            //{ new[] { "scath" },                          "Scathach" },

            //Riders
            //{ new[] { "" },                               "Medusa" },
            { new[] { "george", "fucking invincible" },     "St. George" },
            { new[] { "teach", "blackbeard" },              "Edward Teach" },
            { new[] { "boobyca", "boobdica" },              "Boudica" },
            { new[] { "ushi", "nudist rider" },             "Ushiwakamaru" },
            { new[] { "alex", "shota alex", "shotaskandar" }, "Alexander (Shota)" },
            { new[] { "marie", "sanson's gf", "cake" },     "Marie Antoinette" },
            { new[] { "martha" },                           "St. Martha" },
            { new[] { "drake" },                            "Francis Drake" },
            { new[] { "bonny", "bonny and read", "oppai and loli" }, "Mary Read & Anne Bonney" },

            //Casters
            { new[] { "housewife" },                        "Medea" },
            { new[] { "gilles", "saikou no cool" },         "Gilles de Rais (Caster)" },
            { new[] { "hans", "andersen" },                 "Hans Christian Andersen" },
            { new[] { "shakespeare" },                      "William Shakespeare" },
            { new[] { "mephisto", "clown" },                "Mephistopheles" },
            { new[] { "mozart" },                           "Wolfgang Amadeus Mozart" },
            { new[] { "waver", "el-melloi", "weiba" },      "Zhuge Liang (Lord El-Melloi II)" },
            { new[] { "caster cu", "wickerman" },           "Cu Chulainn (Caster)" },
            { new[] { "caster liz", "casliz" },             "Elizabeth Bathory (Halloween)" },
            { new[] { "casko", "tamamo" },                  "Tamamo no Mae" },
            //{ new[] { "" },                               "Medea Lily" },

            //Assassins
            { new[] { "saski", "savior", "regend" },        "Sasaki Kojirou" },
            { new[] { "hassan" },                           "Hassan of the Cursed Arm" },
            { new[] { "gorgon assassin" },                  "Stheno" },
            //{ new[] { "" },                               "Jing Ke" },
            { new[] { "sanson" },                           "Charles Henri Sanson" },
            { new[] { "phantom", "poto" },                  "Phantom of the Opera" },
            //{ new[] { "" },                               "Mata Hari" },
            //{ new[] { "" },                               "Carmilla" },
            //{ new[] { "" },                               "Jack the Ripper" },

            //Berzerkers
            { new[] { "herc", "hercules" },                 "Herakles" },
            { new[] { "lancebutt", "unrivaled" },           "Lancelot" },
            //{ new[] { "" },                               "Lu Bu Feng Xian" },
            //{ new[] { "" },                               "Spartacus" },
            { new[] { "kintoki" },                          "Sakata Kintoki" },
            { new[] { "vlad" },                             "Vlad III" },
            { new[] { "cowface", "fluffy" },                "Asterios" },
            { new[] { "darius", "giganigga" },              "Darius III" },
            { new[] { "kiyo" },                             "Kiyohime" },
            { new[] { "eric", "erik" },                     "Erik Bloodaxe" },
            { new[] { "nyamamo", "tamacat", "tamamog" },    "Tamamo Cat" },

            { new[] { "jeanne", "ruler" },                  "Jeanne d'Arc" },
            { new[] { "jeanne alter", "ruler alter" },      "Jeanne d'Arc (Alter)" },
            //{ new[] { "" }, "" },
            //{ new[] { "" }, "" },
            //{ new[] { "" }, "" },
            //{ new[] { "" }, "" },
            //{ new[] { "" }, "" },
            //{ new[] { "" }, "" },
        };
    }
}
