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

        private readonly List<ServantProfile> _servantProfiles = new List<ServantProfile>();

        public StatService(IConfiguration config)
        {
            UserCredential credential;
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

            //get table data and serialize to _servantProfiles so that it's cached
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

        public ServantProfile LookupStats(string servant)
        {
            string lookup = String.Empty;
            var key = servantDict.Keys.SingleOrDefault(k => k.Contains(servant.ToLowerInvariant()));

            return (key != null && servantDict.TryGetValue(key, out lookup)) ? 
                _servantProfiles.SingleOrDefault(p => p.Name == lookup) :
                _servantProfiles.SingleOrDefault(p => p.Name == servant);
        }

        public string LookupServantName(string servant)
        {
            string lookup = String.Empty;
            var key = servantDict.Keys.SingleOrDefault(k => k.Contains(servant.ToLowerInvariant()));

            return (key != null && servantDict.TryGetValue(key, out lookup)) ?
                lookup :
                servantDict.Values.Contains(servant, StringComparer.OrdinalIgnoreCase) ?
                servantDict.Values.SingleOrDefault(v => v.ToLowerInvariant() == servant.ToLowerInvariant()) : null;
        }

        internal static IReadOnlyDictionary<string[], string> servantDict = new Dictionary<string[], string>()
        {
            { new[] { "shielder", "mashu" },                "Mash Kyrielight" },

            //Sabers
            { new[] { "saber", "artoria", "arthuria", "king of hungry" }, "Arturia Pendragon" },
            { new[] { "saber alter" },                      "Arturia Pendragon (Alter)" },
            { new[] { "saber lily" },                       "Arturia Pendragon (Lily)" },
            { new[] { "nero", "umu", "emprah" },            "Nero Claudius Ceasar" },
            { new[] { "sieg", "literal shit" },             "Siegfried" },
            { new[] { "ceasar", "fat saber", "faber" },     "Gaius Julius Caesar" },
            { new[] { "jets", "disco", "atilla" },          "Altera" },
            { new[] { "saber gilles", "uncool gilles" },    "Gilles de Rais (Saber)" },
            { new[] { "deon", "trap saber" },               "Le Chevalier d'Eon" },
            { new[] { "okita", "sakusaber" },               "Okita Souji" },
            //{ new[] { "" },                                 "Mordred" },

            //Archers
            { new[] { "emiya", "garcher", "red man" },      "EMIYA" },
            { new[] { "gil", "gilgil", "goldilocks", "auo", "goldie" }, "Gilgamesh" },
            { new[] { "robin" },                            "Robin Hood" },
            { new[] { "nyanta", "atanyanta", "evil cat" },  "Atalanta" },
            { new[] { "gorgon archer" },                    "Euryale" },
            { new[] { "atrash", "aloha snackbar", "trash" }, "Arash" },
            { new[] { "artemis", "tittymonster" },          "Orion"},
            { new[] { "king jew" },                         "David" },
            { new[] { "nobu", "nobunaga" },                 "Oda Nobunaga" },
            //{ new[] { "" },                                 "Arjuna" },
            //{ new[] { "" },                                 "Brynhildr" },

            //Lancers
            { new[] { "cu", "dog", "blue man" },            "Cu Chulainn (Lancer)" },
            { new[] { "liz", "eli", "cutest" },             "Elizabeth Bathory" },
            { new[] { "benkei" },                           "Musashibo Benkei" },
            { new[] { "proto cu", "proto dog" },            "Cu Chulain (Proto)" },
            { new[] { "leo" },                              "Leonidas I" },
            { new[] { "roma" },                             "Romulus" },
            { new[] { "ossan" },                            "Hector" },
            //{ new[] { "scath" },                            "Scathach" },
            //{ new[] { "gil's bff" },                        "Enkidu" },
            //{ new[] { "" },                                 "Karna" },
            //{ new[] { "deermud", "zero lancer" },           "Diarmuid" },

            //Riders
            { new[] { "m'dusa", "responsible adult" },      "Medusa" },
            { new[] { "george", "fucking invincible" },     "St. George" },
            { new[] { "teach", "blackbeard", "neetbeard" }, "Edward Teach" },
            { new[] { "boobyca", "boobdica" },              "Boudica" },
            { new[] { "ushi", "nudist rider" },             "Ushiwakamaru" },
            { new[] { "alex", "shota alex", "shotaskandar" }, "Alexander (Shota)" },
            { new[] { "marie", "sanson's gf", "cake" },     "Marie Antoinette" },
            { new[] { "martha" },                           "St. Martha" },
            { new[] { "drake" },                            "Francis Drake" },
            { new[] { "bonny", "bonny and read", "oppai and loli", "twins" }, "Mary Read & Anne Bonney" },
            //{ new[] { "broskander", "big alex" },           "Alexander the Great" },

            //Casters
            { new[] { "housewife" },                        "Medea" },
            { new[] { "gilles", "saikou no cool" },         "Gilles de Rais (Caster)" },
            { new[] { "hans", "andersen" },                 "Hans Christian Andersen" },
            { new[] { "shakespeare" },                      "William Shakespeare" },
            { new[] { "mephisto", "clown" },                "Mephistopheles" },
            { new[] { "mozart" },                           "Wolfgang Amadeus Mozart" },
            { new[] { "waver", "el-melloi", "weiba" },      "Zhuge Liang (Lord El-Melloi II)" },
            { new[] { "caster cu", "wickerman" },           "Cu Chulainn (Caster)" },
            { new[] { "caster liz", "casliz", "barkley" },  "Elizabeth Bathory (Halloween)" },
            { new[] { "casko", "tamamo", "boss_mog" },      "Tamamo no Mae" },
            { new[] { "loli medea" },                       "Medea Lily" },
            //{ new[] { "alice" },                            "Nursery Rhyme" },

            //Assassins
            { new[] { "sasaki", "savior", "regend" },       "Sasaki Kojirou" },
            { new[] { "hassan" },                           "Hassan of the Cursed Arm" },
            { new[] { "gorgon assassin" },                  "Stheno" },
            { new[] { "chinky" },                           "Jing Ke" },
            { new[] { "sanson" },                           "Charles Henri Sanson" },
            { new[] { "phantom", "poto" },                  "Phantom of the Opera" },
            { new[] { "mata harlot" },                      "Mata Hari" },
            { new[] { "adult liz" },                        "Carmilla" },
            //{ new[] { "jack" },                             "Jack the Ripper" },
            //{ new[] { "" },                                 "Dr. Jekyll" },

            //Berzerkers
            { new[] { "herc", "hercules", "glad in gladiator" }, "Herakles" },
            { new[] { "lancebutt", "unrivaled" },           "Lancelot" },
            { new[] { "do not pursue" },                    "Lu Bu Feng Xian" },
            { new[] { "thunderpants" },                     "Spartacus" },
            { new[] { "kintoki", "slam jam" },              "Sakata Kintoki" },
            { new[] { "vlad", "uncle vlad" },               "Vlad III" },
            { new[] { "cowface", "fluffy" },                "Asterios" },
            { new[] { "darius", "giganigga" },              "Darius III" },
            { new[] { "kiyo" },                             "Kiyohime" },
            { new[] { "eric", "erik" },                     "Erik Bloodaxe" },
            { new[] { "nyamamo", "tamacat", "tamamog" },    "Tamamo Cat" },
            //{ new[] { "" },                                 "Frankenstein" },

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
