using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;
using MechHisui.Modules.WikiModel;
using Newtonsoft.Json.Linq;

namespace MechHisui.Modules
{
    public class Wikier
    {
        public const string BasePath = "http://fategrandorder.wikia.com";
        public const string BaseApi = BasePath + "/api/v1/";

        private readonly RestClient _client;

        public Wikier()
        {
            _client = new RestClient(BaseApi)
            {

            };
        }

        public async Task<ArticleBody> LookupStats(string servant, CancellationToken token)
        {
            string response;
            string lookup = String.Empty;
            var key = servantDict.Keys.Where(k => k.Contains(servant.ToLowerInvariant())).SingleOrDefault();
            try
            {
                if (key != null && servantDict.TryGetValue(key, out lookup))
                {
                    response = await Send(new RestRequest($"Articles/Details?ids=0&titles={lookup}", Method.GET), token);
                    var jObj = JObject.Parse(response);
                    var result = jObj.ToObject<ArticlesDetails>();
                    result.Items = jObj["items"].Children()
                                .OfType<JProperty>()
                                .Select(p => p.Value.ToObject<InnerObject>())
                                .FirstOrDefault();
                    if (result != null)
                    {
                        var article = await Send(new RestRequest($"Articles/AsSimpleJson?id={result.Items.Id}", Method.GET), token);
                        return JObject.Parse(article).ToObject<ArticleBody>();
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    response = await Send(new RestRequest($"Search/List?query={servant}&namespaces=0", Method.GET), token);
                    var jObj = JObject.Parse(response);
                    var result = jObj.ToObject<SearchList>().Items.FirstOrDefault();
                    if (result != null)
                    {
                        var article = await Send(new RestRequest($"Articles/AsSimpleJson?id={result.Id}", Method.GET), token);
                        return JObject.Parse(article).ToObject<ArticleBody>();
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        //public Task<string> Send(HttpMethod method, string path, string json, CancellationToken cancelToken)
        //{
        //    var request = new RestRequest(path, GetMethod(method));
        //    request.AddParameter("application/json", json, ParameterType.RequestBody);
        //    return Send(request, cancelToken);
        //}

        private async Task<string> Send(RestRequest request, CancellationToken cancelToken)
        {
            int retryCount = 0;
            while (true)
            {
                var response = await _client.ExecuteTaskAsync(request, cancelToken).ConfigureAwait(false);
                int statusCode = (int)response.StatusCode;
                if (statusCode == 0) //Internal Error
                {
                    if (response.ErrorException.HResult == -2146233079 && retryCount++ < 5) //The request was aborted: Could not create SSL/TLS secure channel.
                        continue; //Seems to work if we immediately retry
                    throw response.ErrorException;
                }
                if (statusCode < 200 || statusCode >= 300) //2xx = Success
                    throw new Exception(response.StatusCode.ToString());
                return response.Content;
            }
        }

        private Method GetMethod(HttpMethod method)
        {
            switch (method.Method)
            {
                case "DELETE": return Method.DELETE;
                case "GET": return Method.GET;
                case "PATCH": return Method.PATCH;
                case "POST": return Method.POST;
                case "PUT": return Method.PUT;
                default: throw new InvalidOperationException($"Unknown HttpMethod: {method}");
            }
        }

        internal static IReadOnlyDictionary<string[], string> servantDict = new Dictionary<string[], string>()
        {
            { new[] { "shielder", "mashu" },                "Mashu Kyrielite" },

            //Sabers
            { new[] { "saber", "arturia", "arthuria", "king of hungry" }, "Artoria Pendragon" },
            { new[] { "saber alter" },                      "Artoria Pendragon (Alter)" },
            { new[] { "saber lily" },                       "Artoria Pendragon (Lily)" },
            { new[] { "nero", "umu", "emprah" },            "Nero Claudius" },
            { new[] { "sieg", "literal shit" },             "Siegfried" },
            { new[] { "jets" },                             "Attila" },
            { new[] { "ceasar", "fat saber", "faber" },     "Gaius Julius Caesar" },
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
            { new[] { "cu", "dog", "blue man" },            "Cu Chulainn" },
            { new[] { "liz", "eli", "cutest" },             "Elizabeth Bathory" },
            { new[] { "benkei" },                           "Musashibou Benkei" },
            { new[] { "proto cu", "proto dog" },            "Cu Chulainn (Prototype)" },
            { new[] { "leo" },                              "Leonidas" },
            { new[] { "roma" },                             "Romulus" },
            //{ new[] { "" },                               "Hector" },
            //{ new[] { "scath" },                          "Scathach" },

            //Riders
            //{ new[] { "" },                               "Medusa" },
            { new[] { "george", "fucking invincible" },     "Saint George" },
            { new[] { "teach", "blackbeard" },              "Edward Teach" },
            { new[] { "boobyca", "boobdica" },              "Boudica" },
            { new[] { "ushi", "nudist rider" },             "Ushiwakamaru" },
            { new[] { "alex", "shota alex", "shotaskandar" }, "Alexander" },
            { new[] { "marie", "sanson's gf", "cake" },     "Marie Antoinette" },
            { new[] { "martha" },                           "Saint Martha" },
            { new[] { "drake" },                            "Francis Drake" },
            { new[] { "bonny", "bonny and read", "oppai and loli" }, "Anne Bonny & Mary Read" },

            //Casters
            { new[] { "housewife" },                        "Medea" },
            { new[] { "gilles", "saikou no cool" },         "Gilles de Rais" },
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
            { new[] { "hassan" },                           "Cursed Arm Hassan" },
            { new[] { "gorgon assassin" },                  "Stheno" },
            //{ new[] { "" },                               "Jing Ke" },
            { new[] { "sanson" },                           "Charles-Henri Sanson" },
            { new[] { "phantom", "poto" },                  "The Phantom of the Opera" },
            //{ new[] { "" },                               "Mata Hari" },
            //{ new[] { "" },                               "Carmilla" },
            //{ new[] { "" },                               "Jack the Ripper" },

            //Berzerkers
            { new[] { "herc" },                             "Heracles" },
            { new[] { "lancebutt", "unrivaled" },           "Lancelot" },
            //{ new[] { "" },                               "Lu Bu" },
            //{ new[] { "" },                               "Spartacus" },
            { new[] { "kintoki" },                          "Sakata Kintoki" },
            { new[] { "vlad" },                             "Vlad III" },
            { new[] { "cowface", "fluffy" },                "Asterios" },
            { new[] { "darius", "giganigga" },              "Darius III" },
            { new[] { "kiyo" },                             "Kiyohime" },
            { new[] { "eric" },                             "Eric Bloodaxe" },
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
