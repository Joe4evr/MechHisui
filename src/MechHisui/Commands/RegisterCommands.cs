﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Commands;
using Discord.Modules;
using JiiLib;
using Newtonsoft.Json;
using MechHisui.Modules;

namespace MechHisui.Commands
{
    public static class RegisterCommands
    {
        public static void RegisterDeleteCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Delete'...");
            client.GetService<CommandService>().CreateCommand("del")
                .AddCheck((c, u, ch) => (u.Id == UInt64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client)) || ch.Id == u.PrivateChannel.Id)
                .Parameter("number", ParameterType.Required)
                .Hide()
                .Do(async cea =>
                {
                    int n;
                    if (Int32.TryParse(cea.Args[0], out n))
                    {
                        (await cea.Channel.DownloadMessages(limit: 30))
                            .Where(m => m.IsAuthor)
                            .OrderByDescending(m => m.Timestamp)
                            .Take(n)
                            .ToList()
                            .ForEach(async m => await m.Delete());
                    }
                });
        }

        public static void RegisterDisconnectCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Disconnect'...");
            client.GetService<CommandService>().CreateCommand("disconnect")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]))
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

        public static void RegisterImageCommand(this DiscordClient client, IConfiguration config)
        {
            //var ImgurApi = ImgurApiFactory.CreateClient(
            //    Path.Combine(config["Imgur_Secrets_Path"], "imgur_client.json"),
            //    Path.Combine(config["Imgur_Secrets_Path"], "imgur_token.json"));

            
            //Console.WriteLine("Registering 'Image'...");
            //client.GetService<CommandService>().CreateCommand("image")
            //    .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]))
            //    .Hide()
            //    .Parameter("album", ParameterType.Optional)
            //    .Do(async cea =>
            //    {
            //        var imgs = (await client.DownloadMessages(cea.Channel, 20))
            //            .OrderByDescending(m => m.Timestamp)
            //            .FirstOrDefault(m => m.Attachments != null)
            //            .Attachments.Where(a => a.Height != null);

            //        foreach (var img in imgs)
            //        {
                        
            //        }
            //    });
        }

        public static void RegisterInfoCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Info'...");
            client.GetService<CommandService>().CreateCommand("info")
                //.AddCheck((c, u, ch) => Helpers.IsWhilested(ch, client))
                .Description("Relay info about myself.")
                .Do(async cea =>
                {
                    await cea.Channel.SendWithRetry("I am a bot made by Joe4evr. Find my source code here: https://github.com/Joe4evr/MechHisui/ ");
                });
        }

        public static void RegisterKnownChannelsCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Known'...");
            client.GetService<CommandService>().CreateCommand("known")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]))
                .Hide()
                .Do(async cea =>
                {
                    foreach (var channel in Helpers.IterateChannels(client.Servers, printServerNames: true, printChannelNames: false))
                    {
                        Console.WriteLine($"{channel.Name}:  {channel.Id}");
                    }
                    await cea.Channel.SendWithRetry("Known Channel IDs logged to console.");
                });
        }

        public static void RegisterPickCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Pick'...");
            client.GetService<CommandService>().CreateCommand("pick")
                //.AddCheck((c, u, ch) => Helpers.IsWhilested(ch, client))
                .Parameter("items", ParameterType.Multiple)
                .Description("Randomly choose something from any number of items.")
                .Do(async cea =>
                {
                    //Console.WriteLine($"{DateTime.Now}: Command `pick` invoked");
                    if (cea.Args.Length <= 1)
                    {
                        await cea.Channel.SendWithRetry("Provide at least two items.");
                        return;
                    }

                    IEnumerable<string> items = cea.Args.Length == 2 ? cea.Args.RepeatSeq(3) : (cea.Args.Length == 3 ? cea.Args.RepeatSeq(2) : cea.Args);
                    for (int i = 0; i < 28; i++)
                    {
                        items = items.Shuffle();
                    }

                    await cea.Channel.SendWithRetry($"**Picked:** `{items.ElementAt(new Random().Next(maxValue: items.Count()))}`");
                });
        }

        public static void RegisterRecordingCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Recording'...");
            client.GetService<CommandService>().CreateCommand("record")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Hide()
                .Do(async cea =>
                {
                    var rec = client.GetRecordingChannels();
                    if (rec.Contains(cea.Channel.Id))
                    {
                        await cea.Channel.SendWithRetry($"Already recording here.");
                    }
                    else
                    {
                        rec.Add(cea.Channel.Id);
                        var recorder = new Recorder(cea.Channel, client, config);
                        client.GetRecorders().Add(recorder);
                    }
                });

            client.GetService<CommandService>().CreateCommand("endrecord")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Hide()
                .Do(async cea =>
                {
                    var rec = client.GetRecordingChannels();
                    if (!rec.Contains(cea.Channel.Id))
                    {
                        await cea.Channel.SendWithRetry($"Not recording here.");
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
            //Console.WriteLine("Registering 'Reset'...");
            //client.GetService<CommandService>().CreateCommand("reset")
            //    .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
            //    .Hide()
            //    .Do(async cea =>
            //    {
            //        var resp = client.GetResponders().Single(r => r.channel.Id == cea.Channel.Id);
            //        resp.ResetTimeouts();
            //        await cea.Channel.SendWithRetry("Timeouts reset.");
            //    });
        }

        public static void RegisterRollCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Roll'...");
            client.GetService<CommandService>().CreateCommand("roll")
                .Description("Roll one or more dice of variable length. Uses D&D notation.")
                .Parameter("dice", ParameterType.Required)
                .Do(async cea =>
                {
                    if (!Regex.Match(cea.Args[0], "[0-9]+d[0-9]+").Success)
                    {
                        //await cea.Channel.SendWithRetry("**Info:** Previous roll command has been renamed to `gacha`.");
                        await cea.Channel.SendWithRetry("Invalid format specified.");
                        return;
                    }

                    var splits = cea.Args[0].Split('d');
                    int amount;
                    int range;
                    if (Int32.TryParse(splits[0], out amount) && Int32.TryParse(splits[1], out range) && amount > 0 && range > 0)
                    {
                        var rng = new Random();
                        var dice = Enumerable.Range(1, range);
                        var results = new List<int>();
                        for (int i = 0; i < amount; i++)
                        {
                            for (int j = 0; j < 28; j++)
                            {
                                dice = dice.Shuffle();
                            }
                            results.Add(dice.ElementAt(rng.Next(maxValue: range)));
                        }

                        await cea.Channel.SendWithRetry($"**Rolled:** {String.Join(", ", results)}");
                    }

                });
        }

        public static void RegisterThemeCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Theme'...");
            client.GetService<CommandService>().CreateCommand("theme")
                .AddCheck((c, u, ch) => Helpers.IsWhilested(ch, client))
                .Do(async cea =>
                {
                    await cea.Channel.SendWithRetry("https://www.youtube.com/watch?v=mQmgIfP-3OQ");
                });
        }

        public static void RegisterWhereCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Where'...");
            client.GetService<CommandService>().CreateCommand("where")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Parameter("item")
                .Hide()
                .Do(async cea =>
                {
                    ChannelActivity ca;
                    var sb = new StringBuilder();
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
                            //case ChannelActivity.Responder:
                            //    sb.AppendLine("Currently responding in: ");
                            //    foreach (var item in client.GetResponders())
                            //    {
                            //        sb.AppendLine($"{item.channel.Server.Name} - {item.channel.Name}");
                            //    }
                            //    break;
                            //case ChannelActivity.Trivia:
                            //    sb.AppendLine("Currently holding trivia in: ");
                            //    foreach (var item in client.GetTrivias())
                            //    {
                            //        sb.AppendLine($"{item.Channel.Server.Name} - {item.Channel.Name}");
                            //    }
                            //    break;
                            default:
                                break;
                        }
                        var str = sb.ToString();
                        if (!String.IsNullOrWhiteSpace(str))
                        {
                            await cea.Channel.SendWithRetry(str);
                        }
                    }
                    else
                    {
                        await cea.Channel.SendWithRetry("Invalid Argument.");
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
            client.GetService<CommandService>().CreateCommand("xmas")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_general"]) && DateTime.UtcNow.Month == 12)
                .Hide()
                .Do(async cea =>
                {
                    await cea.Channel.SendWithRetry(xmasvids.ElementAt(new Random().Next() % xmasvids.Count));
                });

            client.GetService<CommandService>().CreateCommand("addxmas")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && DateTime.UtcNow.Month == 12)
                .Parameter("item", ParameterType.Required)
                .Hide()
                .Do(async cea =>
                {
                    xmasvids.Add(cea.Args[0]);
                    using (TextWriter tw = new StreamWriter(Path.Combine(config["other"], "xmas.json")))
                    {
                        tw.Write(JsonConvert.SerializeObject(xmasvids, Formatting.Indented));
                    }
                    await cea.Channel.SendWithRetry($"Added `{cea.Args[0]}` to `{nameof(xmasvids)}`.");
                });
        }

        internal static async Task Disconnect(DiscordClient client, IConfiguration config, int code = 0)
        {
            StopReponders(client, client.GetResponders());
            //await StopRecorders(client, client.GetRecorders());
            //await StopTrvias(client.GetTrivias());
            string msg = code == 1 ? "Shutting down for rebuild." : config["Goodbye"];

            foreach (var ch in client.GetService<ModuleService>().Modules
                .Single(m => m.Id == nameof(ChannelWhitelistModule).ToLowerInvariant())
                .EnabledChannels)
            {
                if (ch.Id != UInt64.Parse(config["API_testing"]))
                {
                    await ch.SendWithRetry(msg);
                }
            }

            do await Task.Delay(200);
            while (client.MessageQueue.Count > 0);

            Environment.Exit(code);
        }

        //private static async Task StopTrvias(List<Trivia> trivs)
        //{
        //    if (trivs.Any())
        //    {
        //        foreach (var triv in trivs)
        //        {
        //            await triv.EndTriviaEarly();
        //        }
        //    }
        //}

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

        private static void StopReponders(DiscordClient client, List<ResponderModule> resps)
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
