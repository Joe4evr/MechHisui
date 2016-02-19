using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Commands;
using Discord.Modules;
using JiiLib;
using JiiLib.Net;
using Newtonsoft.Json;
using MechHisui.FateGOLib;
using MechHisui.Modules;
using MechHisui.TriviaService;

namespace MechHisui.Commands
{
    public static class RegisterCommands
    {
        public static void RegisterAddChannelCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Add channel'...");
            client.Services.Get<CommandService>().CreateCommand("add")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Hide()
                .Parameter("id", ParameterType.Required)
                // .Parameter("services", ParameterType.Multiple)
                .Do(async cea =>
                {
                    ulong ch;
                    if (UInt64.TryParse(cea.Args[0], out ch))
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
                        await cea.Channel.SendMessage($"Now listening on channel `{chan.Name}` in `{chan.Server.Name}` until next shutdown.");
                        await chan.SendMessage(config["Hello"]);
                    }
                    else
                    {
                        await cea.Channel.SendMessage("Could not parse channel ID.");
                    }
                });
        }

        public static void RegisterDeleteCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Delete'...");
            client.Services.Get<CommandService>().CreateCommand("del")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
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
            client.Services.Get<CommandService>().CreateCommand("disconnect")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
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

        public static void RegisterEvalCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Eval'...");
            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(StreamReader).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(DiscordClient).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(DateTimeWithZone).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(JsonConvert).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(FgoHelpers).Assembly.Location)
            };
            client.Services.Get<CommandService>().CreateCommand("eval")
                .Parameter("func", ParameterType.Unparsed)
                .Hide()
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) || ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"]))
                .Do(async cea =>
                {
                    string arg = cea.Args[0]; //.Replace("\\", String.Empty);
                    if (arg.Contains('^'))
                    {
                        await cea.Channel.SendMessage("**Note:** `^` is the Binary XOR operator. Use `Math.Pow(base, exponent)` if you wish to calculate an exponentiation.");
                    }
                    if (!arg.Contains("return"))
                    {
                        arg = $"return {arg}";
                    }
                    if (!arg.EndsWith(";"))
                    {
                        arg += ';';
                    }
                    SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
$@"using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Discord;
using JiiLib;
using Newtonsoft.Json;
using MechHisui.FateGOLib;

namespace DynamicCompile
{{
    public class DynEval
    {{
        public string Eval<T>(Func<IEnumerable<T>> set) => String.Join("", "", set());

        public string Eval<T>(Func<T> func) => func()?.ToString() ?? ""null"";

        public string Exec() => Eval(() => {{ {arg} }});
    }}
}}");

                    string assemblyName = Path.GetRandomFileName();
                    CSharpCompilation compilation = CSharpCompilation.Create(
                        assemblyName: assemblyName,
                        syntaxTrees: new[] { syntaxTree },
                        references: references,
                        options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                    using (var ms = new MemoryStream())
                    {
                        EmitResult result = compilation.Emit(ms);

                        if (!result.Success)
                        {
                            IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                                diagnostic.IsWarningAsError ||
                                diagnostic.Severity == DiagnosticSeverity.Error);

                            Console.Error.WriteLine(String.Join("\n", failures.Select(f => $"{f.Id}: {f.GetMessage()}")));
                            await cea.Channel.SendMessage($"**Error:** {failures.First().GetMessage()}");
                        }
                        else
                        {
                            ms.Seek(0, SeekOrigin.Begin);
                            Assembly assembly = Assembly.Load(ms.ToArray());

                            Type type = assembly.GetType("DynamicCompile.DynEval");
                            object obj = Activator.CreateInstance(type);
                            var res = type.InvokeMember("Exec",
                                BindingFlags.Default | BindingFlags.InvokeMethod,
                                null,
                                obj,
                                new object[0]);

                            await cea.Channel.SendMessage($"**Result:** {(string)res}");
                        }
                    }
                });
        }

        public static void RegisterImageCommand(this DiscordClient client, IConfiguration config)
        {
            //var ImgurApi = ImgurApiFactory.CreateClient(
            //    Path.Combine(config["Imgur_Secrets_Path"], "imgur_client.json"),
            //    Path.Combine(config["Imgur_Secrets_Path"], "imgur_token.json"));

            
            //Console.WriteLine("Registering 'Image'...");
            //client.Services.Get<CommandService>().CreateCommand("image")
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
            client.Services.Get<CommandService>().CreateCommand("info")
                .AddCheck((c, u, ch) => Helpers.IsWhilested(ch, client))
                .Description("Relay info about myself.")
                .Do(async cea =>
                {
                    await cea.Channel.SendMessage("I am a bot made by Joe4evr. Find my source code here: https://github.com/Joe4evr/MechHisui/ ");
                });
        }

        public static void RegisterKnownChannelsCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Known'...");
            client.Services.Get<CommandService>().CreateCommand("known")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Hide()
                .Do(async cea =>
                {
                    foreach (var channel in Helpers.IterateChannels(client.Servers, printServerNames: true, printChannelNames: false))
                    {
                        Console.WriteLine($"{channel.Name}:  {channel.Id}");
                    }
                    await cea.Channel.SendMessage("Known Channel IDs logged to console.");
                });
        }

        public static void RegisterLearnCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Learn'...");
            client.Services.Get<CommandService>().CreateCommand("learn")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
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
                    await cea.Channel.SendMessage($"Understood. Shall respond to `{triggger}` with `{response}`.");
                });
        }

        public static void RegisterPickCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Pick'...");
            client.Services.Get<CommandService>().CreateCommand("pick")
                .AddCheck((c, u, ch) => Helpers.IsWhilested(ch, client))
                .Parameter("items", ParameterType.Multiple)
                .Description("Randomly choose something from any number of items.")
                .Do(async cea =>
                {
                    if (cea.Args.Length <= 1)
                    {
                        await cea.Channel.SendMessage("Provide at least two items.");
                        return;
                    }

                    var items = cea.Args.ToList();
                    for (int i = 0; i < 28; i++)
                    {
                        items.Shuffle();
                    }

                    await cea.Channel.SendMessage($"**Picked:** `{items.ElementAt(new Random().Next(maxValue: items.Count))}`");
                });
        }

        public static void RegisterRecordingCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Recording'...");
            client.Services.Get<CommandService>().CreateCommand("record")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Hide()
                .Do(async cea =>
                {
                    var rec = client.GetRecordingChannels();
                    if (rec.Contains(cea.Channel.Id))
                    {
                        await cea.Channel.SendMessage($"Already recording here.");
                    }
                    else
                    {
                        rec.Add(cea.Channel.Id);
                        var recorder = new Recorder(cea.Channel, client, config);
                        client.GetRecorders().Add(recorder);
                    }
                });

            client.Services.Get<CommandService>().CreateCommand("endrecord")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Hide()
                .Do(async cea =>
                {
                    var rec = client.GetRecordingChannels();
                    if (!rec.Contains(cea.Channel.Id))
                    {
                        await cea.Channel.SendMessage($"Not recording here.");
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
            client.Services.Get<CommandService>().CreateCommand("reset")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Hide()
                .Do(async cea =>
                {
                    var resp = client.GetResponders().Single(r => r.channel.Id == cea.Channel.Id);
                    resp.ResetTimeouts();
                    await cea.Channel.SendMessage("Timeouts reset.");
                });
        }

        public static void RegisterThemeCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Theme'...");
            client.Services.Get<CommandService>().CreateCommand("theme")
                .AddCheck((c, u, ch) => Helpers.IsWhilested(ch, client))
                .Do(async cea =>
                {
                    await cea.Channel.SendMessage("https://www.youtube.com/watch?v=mQmgIfP-3OQ");
                });
        }

        public static void RegisterWhereCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Where'...");
            client.Services.Get<CommandService>().CreateCommand("where")
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
                            await cea.Channel.SendMessage(str);
                        }
                    }
                    else
                    {
                        await cea.Channel.SendMessage("Invalid Argument.");
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
            client.Services.Get<CommandService>().CreateCommand("xmas")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_general"]) && DateTime.UtcNow.Month == 12)
                .Hide()
                .Do(async cea =>
                {
                    await cea.Channel.SendMessage(xmasvids.ElementAt(new Random().Next() % xmasvids.Count));
                });

            client.Services.Get<CommandService>().CreateCommand("addxmas")
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
                    await cea.Channel.SendMessage($"Added `{cea.Args[0]}` to `{nameof(xmasvids)}`.");
                });
        }

        internal static async Task Disconnect(DiscordClient client, IConfiguration config, int code = 0)
        {
            StopReponders(client, client.GetResponders());
            //await StopRecorders(client, client.GetRecorders());
            //await StopTrvias(client.GetTrivias());
            string msg = code == 1 ? "Shutting down for rebuild." : config["Goodbye"];

            foreach (var ch in client.Modules().Modules
                .Single(m => m.Id == nameof(ChannelWhitelistModule).ToLowerInvariant())
                .EnabledChannels)
            {
                if (ch.Id != UInt64.Parse(config["API_testing"]))
                {
                    await ch.SendMessage(msg);
                }
            }

            do await Task.Delay(200);
            while (client.MessageQueue.Count > 0);

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

    public class DynEval
    {
        public string Eval<T>(Func<IEnumerable<T>> set) => String.Join(", ", set.Invoke());

        public string Eval<T>(Func<T> func) => func.Invoke().ToString();

        public string Exec() => Eval(() => { return Enumerable.Range(1, 10); });
    }
}
