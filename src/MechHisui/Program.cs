using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using Discord;
using Discord.Commands;
using Discord.Modules;
using MechHisui.FateGOLib.Modules;
using MechHisui.Commands;
using MechHisui.HisuiBets;
using MechHisui.Modules;

namespace MechHisui
{
    public class Program
    {
        public static void Main(string[] args)
        {
            PlatformServices ps = PlatformServices.Default;
            var env = ps.Application;
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .SetBasePath(Path.Combine(env.ApplicationBasePath, @"..\..\..\..\"));

            if (args.Contains("--debug"))
            {
                Console.WriteLine("Loading from UserSecret store");
                builder.AddUserSecrets();
            }
            else
            {
                Console.WriteLine("Loading from jsons directory");
                builder.AddInMemoryCollection(JsonConvert.DeserializeObject<Dictionary<string, string>>(
                    File.ReadAllText(@"..\MechHisui-jsons\secrets.json")
                ));
            }

            //var dbContext = new MechHisuiDbContext();

            IConfiguration config = builder.Build();

            var client = new DiscordClient(conf =>
            {
                conf.AppName = "MechHisui";
                conf.AppVersion = "0.3.0";
                conf.LogLevel = LogSeverity.Info;
                //conf.UseLargeThreshold = true;
            });

            //client.Disconnected += (s, e) => Environment.Exit(0);
            Console.CancelKeyPress += async (s, e) => await RegisterCommands.Disconnect(client, config);

            //Display all log messages in the console
            client.Log.Message += (s, e) =>
            {
                if (!e.Message.Contains("Discord API (Unofficial)/"))
                {
                    Console.WriteLine($"{DateTime.Now} - [{e.Severity}] {e.Source}: {e.Message} {e.Exception}");
                }
            };

            //Add a ModuleService and CommandService
            client.AddService(new ModuleService());
            client.UsingCommands(conf =>
            {
                conf.AllowMentionPrefix = true;
                conf.HelpMode = HelpMode.Public;
                conf.PrefixChar = '.';
            });

            //register commands
            //client.RegisterAddChannelCommand(config);
            //client.RegisterDeleteCommand(config);
            client.RegisterDisconnectCommand(config);

            var evalBuilder = EvalModule.Builder.BuilderWithSystemAndLinq()
                .Add(new EvalReference(MetadataReference.CreateFromFile(typeof(DiscordClient).Assembly.Location), "Discord"))
                .Add(new EvalReference(MetadataReference.CreateFromFile(typeof(CommandEventArgs).Assembly.Location), "Discord.Commands"))
                .Add(new EvalReference(MetadataReference.CreateFromFile(typeof(JiiLib.Extensions).Assembly.Location), "JiiLib"))
                .Add(new EvalReference(MetadataReference.CreateFromFile(typeof(FateGOLib.FgoHelpers).Assembly.Location), "MechHisui.FateGOLib"));

            client.AddModule(evalBuilder.Build((c, u, ch)
                => u.Id == UInt64.Parse(config["Owner"]) || ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"])));

            //client.RegisterImageCommand(config);
            client.RegisterInfoCommand(config);
            //client.RegisterKnownChannelsCommand(config);
            //client.RegisterLearnCommand(config);
            client.RegisterPickCommand(config);
            //client.RegisterRecordingCommand(config);
            //client.RegisterResetCommand(config);
            client.RegisterRollCommand(config);
            client.RegisterThemeCommand(config);
            //client.RegisterWhereCommand(config);
            //client.RegisterXmasCommand(config);

            client.RegisterAPCommand(config);
            client.RegisterDailyCommand(config);
            client.AddModule(new FriendsModule(config["FriendcodePath"],
                (c, u, ch) => ch.Id == UInt64.Parse(config["FGO_playground"])));
            client.RegisterLoginBonusCommand(config);

            new FgoStatsMetaModule(config).InstallModules(client);
            //client.RegisterStatsCommands(config);
            client.RegisterQuartzCommand(config);
            client.RegisterZoukenCommand(config);

            client.AddModule(new HisuiBetsModule(client, config));
            //client.RegisterHisuiBetsCommands(config);

            client.RegisterSecretHitler(config);

            //client.RegisterTriviaCommand(config);

            var rm = new ResponderModule(config);
            client.AddModule(rm);

            int lastcode = 0;
            if (args.Length > 0 && Int32.TryParse(args[0], out lastcode) && lastcode != 0)
            {
                Console.WriteLine($"Last exit code was {lastcode}");
            }

            //client.MessageReceived += async (s, e) =>
            //{
            //    if (e.Message.MentionedUsers.Select(m => m.Id).Contains(UInt64.Parse(config["Owner"]))
            //        && e.Server.GetUser(UInt64.Parse(config["Owner"])).Status == UserStatus.Offline)
            //    {
            //        var text = e.Message.Text.Replace('@', '~');
            //        await client.GetChannel(UInt64.Parse(config["PrivChat"])).SendMessage($"You were pinged at **{DateTime.Now}** in **{e.Server.Name}/{e.Channel.Name}** by **{e.User.Name}**:\n\"{text}\"");
            //    }
            //};

            client.MessageUpdated += async (s, e) =>
            {
                if (!(await e.Channel.DownloadMessages(10, e.Before.Id, Relative.After)).Any(m => m.IsAuthor))
                {
                    var msgReceived = typeof(DiscordClient).GetMethod("OnMessageReceived", BindingFlags.NonPublic | BindingFlags.Instance);
                    msgReceived.Invoke(s, new object[] { e.After });
                    //client.OnMessageReceived(e.After);
                }
            };

            try
            {
                //Convert our sync method to an async one and block the Main function until the bot disconnects
                client.ExecuteAndWait(async () =>
                {
                    await client.Connect(config["LoginToken"]);
                    Console.WriteLine($"Logged in as {client.CurrentUser.Name}");
                    Console.WriteLine($"MH v. 0.3.0");

                    //Use a channel whitelist
                    client.GetService<ModuleService>().Add(
                            new ChannelWhitelistModule(
                                Helpers.ConvertStringArrayToULongArray(
                                    //config["API_testing"]
                                    //config["LTT_general"],
                                    //config["LTT_testing"],
                                    config["FGO_playground"],
                                    config["FGO_Hgames"],
                                    config["FGO_events"],
                                    config["FGO_general"]
                                )
                            ),
                            nameof(ChannelWhitelistModule),
                            ModuleFilter.ChannelWhitelist
                        );
                    await Task.Delay(3000);
                    if (!client.Servers.Any())
                    {
                        Console.WriteLine("Not a member of any server");
                    }
                    else
                    {
                        foreach (var channel in Helpers.IterateChannels(client.Servers, printServerNames: true, printChannelNames: true))
                        {
                            if (!channel.IsPrivate && Helpers.IsWhilested(channel, client))
                            {
                                //Console.CancelKeyPress += async (s, e) => await client.SendMessage(channel, config["Goodbye"]);
                                if (channel.Id != UInt64.Parse(config["API_testing"]))
                                {
                                    if (Debugger.IsAttached)
                                    {
                                        // await channel.SendMessage("MechHisui started in debug mode. Not all commands will be available.");
                                    }
                                    else if (lastcode != -1 && channel.Id != UInt64.Parse(config["FGO_events"]))
                                    {
                                        await channel.SendMessage(config["Hello"]);
                                    }
                                }
                            }
                        }
                        client.GetModule<HisuiBetsModule>().Instance.AddNewHisuiBetsUsers(client, config);
                        Console.WriteLine($"Started up at {DateTime.Now}.");
                    }
                });
            }
            catch (Exception ex)
            {
                File.AppendAllText(Path.Combine(config["Logs"], "crashlogs.txt"), $"{DateTime.Now} - {ex.Message}\n{ex.StackTrace}\n");
                Environment.Exit(-1);
            }
        }
    }
}
