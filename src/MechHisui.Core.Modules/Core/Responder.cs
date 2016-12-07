//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using Newtonsoft.Json;
//using JiiLib;
//using Discord;
//using Discord.Commands;

//namespace MechHisui.Modules
//{
//    public class ResponderModule : ModuleBase
//    {
//        private ConcurrentDictionary<string[], DateTime> _lastResponses = new ConcurrentDictionary<string[], DateTime>();
//        private readonly IConfiguration _config;

//        public ResponderModule(IConfiguration config)
//        {
//            _config = config;
//        }

//        internal void ResetTimeouts() => _lastResponses = new ConcurrentDictionary<string[], DateTime>();

//        internal async void Respond(object sender, MessageEventArgs e)
//        {
//            string temp = (e?.Message?.Text?.StartsWith("@") ?? false
//                ? new string(e.Message.Text.SkipWhile(c => !Char.IsWhiteSpace(c)).ToArray())
//                : e.Message.Text);

//            if (!String.IsNullOrEmpty(temp))
//            {
//                string quickResponse = String.Empty;
//                Func<Response, bool> pred = (k => k?.Call?.ContainsIgnoreCase(temp.Trim()) ?? false);
//                var resp = Responses.responseDict.SingleOrDefault(k => k.Key?.ContainsIgnoreCase(temp.Trim()) ?? false);
//                var sResp = Responses.spammableResponses.SingleOrDefault(pred);

//                if (resp.Key != null)
//                {
//                    DateTime last;
//                    var msgTime = e.Message.Timestamp.ToUniversalTime();
//                    if (!_lastResponses.TryGetValue(resp.Key, out last) || (DateTime.UtcNow - last) > TimeSpan.FromMinutes(1))
//                    {
//                        _lastResponses.AddOrUpdate(resp.Key, msgTime, (k, v) => v = msgTime);
//                        await e.Channel.SendMessage(resp.Value[new Random().Next(maxValue: resp.Value.Length)]);
//                    }
//                }
//                else if (sResp != null)
//                {
//                    await e.Channel.SendMessage(sResp.Resp[new Random().Next(maxValue: sResp.Resp.Length)]);
//                }
//            }
//        }

//        void IModule.Install(ModuleManager manager)
//        {
//            Console.WriteLine("Initializing Responder...");
//            Responses.InitResponses(_config);

//            Console.WriteLine("Registering 'Learn'...");
//            manager.Client.GetService<CommandService>().CreateCommand("learn")
//                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(_config["Owner"]))
//                .Parameter("trigger", ParameterType.Required)
//                .Parameter("response", ParameterType.Required)
//                .Parameter("kind", ParameterType.Optional)
//                .Hide()
//                .Do(async cea =>
//                {
//                    string triggger = cea.Args[0];
//                    string response = cea.Args[1];
//                    //var response = new Response { Call = new[] { cea.Args[0] }, Resp = new[] { cea.Args[1] } };
//                    Responses.responseDict.AddOrUpdate(
//                        Responses.responseDict.SingleOrDefault(kv => kv.Key.Contains(triggger)).Key ?? new string[] { triggger },
//                        new string[] { response },
//                        (k, v) =>
//                        {
//                            var t = v.ToList();
//                            t.Add(response);
//                            return t.ToArray();
//                        });
//                    using (TextWriter tw = new StreamWriter(_config["ResponsesPath"]))
//                    {
//                        var l = new List<Response>();
//                        foreach (var item in Responses.responseDict)
//                        {
//                            l.Add(new Response { Call = item.Key, Resp = item.Value });
//                        }
//                        tw.Write(JsonConvert.SerializeObject(l, Formatting.Indented));
//                    }
//                    await cea.Channel.SendMessage($"Understood. Shall respond to `{triggger}` with `{response}`.");
//                });

//            manager.Client.GetService<CommandService>().CreateCommand("refresh")
//                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(_config["Owner"]))
//                .Hide()
//                .Do(async cea =>
//                {
//                    Responses.InitResponses(_config);
//                    await cea.Channel.SendMessage("Refreshed auto-responses.");
//                });

//            manager.Client.MessageReceived += Respond;
//        }
//    }

//    public class Response
//    {
//        public string[] Call { get; set; }
//        public string[] Resp { get; set; }
//    }

//    internal static class Responses
//    {
//        public static void InitResponses(IConfiguration config)
//        {
//            responseDict = new ConcurrentDictionary<string[], string[]>();
//            var temp = JsonConvert.DeserializeObject<List<Response>>(File.ReadAllText(config["ResponsesPath"])) ?? new List<Response>();
//            foreach (var item in temp)
//            {
//                responseDict.AddOrUpdate(item.Call, item.Resp, (k, v) => v = item.Resp);
//            }
            
//            spammableResponses = JsonConvert.DeserializeObject<List<Response>>(File.ReadAllText(config["SpamResponsesPath"])) ?? new List<Response>();
//        }

//        internal static ConcurrentDictionary<string[], string[]> responseDict;

//        internal static List<Response> spammableResponses;
//    }
//}
