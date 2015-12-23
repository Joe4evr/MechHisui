using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Commands;
using Discord.Modules;
using MechHisui.Modules;
using Newtonsoft.Json;
using MechHisui.TriviaService;

namespace MechHisui.Commands
{
    public static class RegisterCommands
    {
        public static void RegisterAddChannelCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Add channel'...");
            client.Commands().CreateCommand("add")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Hide()
                .Parameter("id", ParameterType.Required)
                // .Parameter("services", ParameterType.Multiple)
                .Do(async cea =>
                {
                    long ch;
                    if (Int64.TryParse(cea.Args[0], out ch))
                    {
                        Channel chan = client.GetChannel(ch);
                        client.Modules().Modules
                            .SingleOrDefault(m => m.Id == nameof(ChannelWhitelistModule).ToLowerInvariant())
                            .EnableChannel(chan);

                        //if (cea.Args.Length > 1)
                        //{
                        //    List<string> services = new List<string>();
                        //    for (int i = 1; i < cea.Args.Length; i++)
                        //    {
                        //        if (cea.Args[i] == nameof(Responder).ToLowerInvariant())
                        //        {
                        //            client.MessageReceived += (new Responder(chan, client).Respond);
                        //        }
                        //        else if (cea.Args[i] == nameof(Recorder).ToLowerInvariant())
                        //        {

                        //        }
                        //        else
                        //        {
                        //            continue;
                        //        }
                        //        services.Add(cea.Args[i]);
                        //    }

                        //    await client.SendMessage(cea.Channel, $"Now listening on channel {chan.Name} in {chan.Server.Name} with {String.Join(", ", services)} until next shutdown.");
                        //}
                        //else
                        //{
                        //    await client.SendMessage(cea.Channel, $"Now listening on channel {chan.Name} in {chan.Server.Name} until next shutdown.");
                        //}

                        client.MessageReceived += (new Responder(chan, client).Respond);
                        await client.SendMessage(cea.Channel, $"Now listening on channel `{chan.Name}` in `{chan.Server.Name}` until next shutdown.");
                        await client.SendMessage(chan, config["Hello"]);
                    }
                    else
                    {
                        await client.SendMessage(cea.Channel, "Could not parse channel ID.");
                    }
                });
        }

        public static void RegisterDisconnectCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Disconnect'...");
            client.Commands().CreateCommand("disconnect")
                .AddCheck((c, u, ch) => u.Id == long.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Parameter("code", ParameterType.Optional)
                .Hide()
                .Do(async cea =>
                {
                    int c;
                    if (!String.IsNullOrEmpty(cea.Args[0]) && Int32.TryParse(cea.Args[0], out c))
                    {
                        await Disconnect(client, config, c);
                    }
                    await Disconnect(client, config);
                });
        }

        public static void RegisterInfoCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Info'...");
            client.Commands().CreateCommand("info")
                .AddCheck((c, u, ch) => Helpers.IsWhilested(ch, client))
                .Description("Relay info about myself.")
                .Do(async cea =>
                {
                    await client.SendMessage(cea.Channel, "I am a bot made by Joe4evr. Find my source code here: https://github.com/Joe4evr/MechHisui/ ");
                });
        }

        public static void RegisterKnownChannelsCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Known'...");
            client.Commands().CreateCommand("known")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Hide()
                .Do(async cea =>
                {
                    foreach (var channel in Helpers.IterateChannels(client.AllServers, printServerNames: true, printChannelNames: false))
                    {
                        Console.WriteLine($"{channel.Name}:  {channel.Id}");
                    }
                    await client.SendMessage(cea.Channel, "Known Channel IDs logged to console.");
                });
        }

        public static void RegisterLearnCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Learn'...");
            client.Commands().CreateCommand("learn")
                .AddCheck((c, u, ch) => u.Id == long.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Parameter("trigger", ParameterType.Required)
                .Parameter("response", ParameterType.Required)
                .Parameter("kind", ParameterType.Optional)
                .Hide()
                .Do(async cea =>
                {
                    string triggger = cea.Args[0];
                    string response = cea.Args[1];
                    //var response = new Response { Call = new[] { cea.Args[0] }, Resp = new[] { cea.Args[1] } };
                    Responses.responseDict.AddOrUpdate(
                        Responses.responseDict.SingleOrDefault(kv => kv.Key.Contains(triggger)).Key ?? new string[] { triggger },
                        new string[] { response },
                        (k, v) =>
                        {
                            var t = v.ToList();
                            t.Add(response);
                            return t.ToArray();
                        });
                    using (TextWriter tw = new StreamWriter(config["ResponsesPath"]))
                    {
                        var l = new List<Response>();
                        foreach (var item in Responses.responseDict)
                        {
                            l.Add(new Response { Call = item.Key, Resp = item.Value });
                        }
                        tw.Write(JsonConvert.SerializeObject(l, Formatting.Indented));
                    }
                    await client.SendMessage(cea.Channel, $"Understood. Shall respond to `{triggger}` with `{response}`.");
                });
        }

        public static void RegisterMarkCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Mark'...");
            client.Commands().CreateCommand("mark")
                .AddCheck((c, u, ch) => u.Id == long.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Hide()
                .Do(async cea =>
                {
                    Console.WriteLine($"Marked at {DateTime.Now}");
                    await client.SendMessage(cea.Channel, "Marked current activity in the console.");
                });
        }

        public static void RegisterRecordingCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Recording'...");
            client.Commands().CreateCommand("record")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Hide()
                .Do(async cea =>
                {
                    var rec = client.GetRecordingChannels();
                    if (rec.Contains(cea.Channel.Id))
                    {
                        await client.SendMessage(cea.Channel, $"Already recording here.");
                    }
                    else
                    {
                        rec.Add(cea.Channel.Id);
                        var recorder = new Recorder(cea.Channel, client, config);
                        client.GetRecorders().Add(recorder);
                    }
                });

            client.Commands().CreateCommand("endrecord")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Hide()
                .Do(async cea =>
                {
                    var rec = client.GetRecordingChannels();
                    if (!rec.Contains(cea.Channel.Id))
                    {
                        await client.SendMessage(cea.Channel, $"Not recording here.");
                    }
                    else
                    {
                        var recorder = client.GetRecorders().Single(r => r.channel.Id == cea.Channel.Id);
                        await recorder.EndRecord(client);
                        client.GetRecorders().Remove(recorder);
                        rec.Remove(cea.Channel.Id);
                    }
                });
        }

        public static void RegisterResetCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Reset'...");
            client.Commands().CreateCommand("reset")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Hide()
                .Do(async cea =>
                {
                    var resp = client.GetResponders().Single(r => r.channel.Id == cea.Channel.Id);
                    resp.ResetTimeouts();
                    await client.SendMessage(cea.Channel, "Timeouts reset.");
                });
        }

        public static void RegisterThemeCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Theme'...");
            client.Commands().CreateCommand("theme")
                .AddCheck((c, u, ch) => Helpers.IsWhilested(ch, client))
                .Do(async cea =>
                {
                    await client.SendMessage(cea.Channel, "https://www.youtube.com/watch?v=mQmgIfP-3OQ");
                });
        }

        public static void RegisterWhereCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Where'...");
            client.Commands().CreateCommand("where")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Parameter("item")
                .Hide()
                .Do(async cea =>
                {
                    ChannelActivity ca;
                    StringBuilder sb = new StringBuilder();
                    if (Enum.TryParse(cea.Args[0], ignoreCase: true, result: out ca))
                    {
                        switch (ca)
                        {
                            case ChannelActivity.Recorder:
                                sb.AppendLine("Currently recording in: ");
                                foreach (var item in client.GetRecorders())
                                {
                                    sb.AppendLine($"{item.channel.Server.Name} - {item.channel.Name}");
                                }
                                break;
                            case ChannelActivity.Responder:
                                sb.AppendLine("Currently responding in: ");
                                foreach (var item in client.GetResponders())
                                {
                                    sb.AppendLine($"{item.channel.Server.Name} - {item.channel.Name}");
                                }
                                break;
                            case ChannelActivity.Trivia:
                                sb.AppendLine("Currently holding trivia in: ");
                                foreach (var item in client.GetTrivias())
                                {
                                    sb.AppendLine($"{item.Channel.Server.Name} - {item.Channel.Name}");
                                }
                                break;
                            default:
                                break;
                        }
                        var str = sb.ToString();
                        if (!String.IsNullOrWhiteSpace(str))
                        {
                            await client.SendMessage(cea.Channel, str);
                        }
                    }
                    else
                    {
                        await client.SendMessage(cea.Channel, "Invalid Argument.");
                    }
                });
        }

        public static void RegisterXmasCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Xmas'...");
            using (TextReader tr = new StreamReader(Path.Combine(config["other"], "xmas.json")))
            {
                xmasvids = JsonConvert.DeserializeObject<List<string>>(tr.ReadToEnd());
            }
            client.Commands().CreateCommand("xmas")
                .AddCheck((c, u, ch) => ch.Id == long.Parse(config["FGO_general"]) && DateTime.UtcNow.Month == 12)
                .Hide()
                .Do(async cea =>
                {
                    await client.SendMessage(cea.Channel, xmasvids.ElementAt(new Random().Next() % xmasvids.Count));
                });

            client.Commands().CreateCommand("addxmas")
                .AddCheck((c, u, ch) => u.Id == long.Parse(config["Owner"]) && DateTime.UtcNow.Month == 12)
                .Parameter("item", ParameterType.Required)
                .Hide()
                .Do(async cea =>
                {
                    xmasvids.Add(cea.Args[0]);
                    using (TextWriter tw = new StreamWriter(Path.Combine(config["other"], "xmas.json")))
                    {
                        tw.Write(JsonConvert.SerializeObject(xmasvids, Formatting.Indented));
                    }
                    await client.SendMessage(cea.Channel, $"Added `{cea.Args[0]}` to `{nameof(xmasvids)}`.");
                });
        }

        internal static async Task Disconnect(DiscordClient client, IConfiguration config, int code = 0)
        {
            StopReponders(client, client.GetResponders());
            await StopRecorders(client, client.GetRecorders());
            //await StopTrvias(client.GetTrivias());

            foreach (var ch in client.Modules().Modules
                .Single(m => m.Id == nameof(ChannelWhitelistModule).ToLowerInvariant())
                .EnabledChannels)
            {
                if (ch.Id != Int64.Parse(config["API_testing"]) && ch.Id != Int64.Parse(config["FGO_trivia"]))
                {
                    string msg = code == 1 ? "Shutting down for rebuild." : config["Goodbye"];
                    await client.SendMessage(ch, msg);
                }
            }

            Environment.Exit(code);
        }

        private static async Task StopTrvias(List<Trivia> trivs)
        {
            if (trivs.Any())
            {
                foreach (var triv in trivs)
                {
                    await triv.EndTriviaEarly();
                }
            }
        }

        private static async Task StopRecorders(DiscordClient client, List<Recorder> recs)
        {
            if (recs.Any())
            {
                foreach (var rec in recs)
                {
                    await rec.EndRecord(client);
                }
            }
        }

        private static void StopReponders(DiscordClient client, List<Responder> resps)
        {
            if (resps.Any())
            {
                foreach (var resp in resps)
                {
                    client.MessageReceived -= resp.Respond;
                }
            }
        }

        private static List<string> xmasvids = new List<string>();

        private enum ChannelActivity
        {
            Recorder,
            Responder,
            Trivia
        }
    }
}
