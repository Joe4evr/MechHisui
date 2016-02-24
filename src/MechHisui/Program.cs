using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.PlatformAbstractions;
using Discord;
using Discord.Commands;
using Discord.Modules;
using MechHisui.Commands;
using MechHisui.Modules;

namespace MechHisui
{
    public class Program
    {
        public static void Main(string[] args)
        {
            PlatformServices ps = PlatformServices.Default;
            IApplicationEnvironment env = ps.Application;
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .SetBasePath(env.ApplicationBasePath);

            IHostingEnvironment hostingEnv = new HostingEnvironment();
            hostingEnv.Initialize(env.ApplicationBasePath, builder.Build());
            if (hostingEnv.IsDevelopment())
            {
                Console.WriteLine("Loading from UserSecret store");
                builder.AddUserSecrets();
            }
            else
            {
                Console.WriteLine("Loading from jsons directory");
                builder.AddJsonFile(@"..\..\..\..\..\..\MechHisui-jsons\secrets.json");
            }

            IConfiguration config = builder.Build();

            var client = new DiscordClient(conf =>
            {
                conf.AppName = "MechHisui";
                conf.AppVersion = "0.3.0";
                conf.LogLevel = LogSeverity.Warning;
                //conf.UseLargeThreshold = true;
            });

            //client.Disconnected += (s, e) => Environment.Exit(0);
            Console.CancelKeyPress += async (s, e) => await RegisterCommands.Disconnect(client, config);

            //Display all log messages in the console
            client.Log.Message += (s, e) => Console.WriteLine($"{DateTime.Now} - [{e.Severity}] {e.Source}: {e.Message} {e.Exception}");

            //Add a ModuleService and CommandService
            client.AddService(new ModuleService());
            client.UsingCommands(conf =>
            {
                conf.AllowMentionPrefix = true;
                conf.HelpMode = HelpMode.Public;
                conf.PrefixChar = '.';
            });

            //register commands
            client.RegisterAddChannelCommand(config);
            client.RegisterDeleteCommand(config);
            client.RegisterDisconnectCommand(config);
            if (!Debugger.IsAttached)
            {
                client.RegisterEvalCommand(config);
            }
            //client.RegisterImageCommand(config);
            client.RegisterInfoCommand(config);
            client.RegisterKnownChannelsCommand(config);
            client.RegisterLearnCommand(config);
            client.RegisterPickCommand(config);
            //client.RegisterRecordingCommand(config);
            client.RegisterResetCommand(config);
            client.RegisterThemeCommand(config);
            client.RegisterWhereCommand(config);
            client.RegisterXmasCommand(config);

            client.RegisterAPCommand(config);
            client.RegisterDailyCommand(config);
            client.RegisterEventCommand(config);
            client.RegisterFriendsCommand(config);
            client.RegisterLoginBonusCommand(config);
            if (!Debugger.IsAttached)
            {
                client.RegisterStatsCommands(config);
            }
            client.RegisterQuartzCommand(config);
            client.RegisterZoukenCommand(config);
            
            client.RegisterHisuiBetsCommands(config);

            client.RegisterSecretHitler(config);

            client.RegisterTriviaCommand(config);

            Responses.InitResponses(config);

            client.MessageUpdated += async (s, e) =>
            {
                if (!(await e.Channel.DownloadMessages(10, e.Before.Id, Relative.After)).Any(m => m.IsAuthor))
                {
                    var msgReceived = typeof(DiscordClient).GetMethod("OnMessageReceived", BindingFlags.NonPublic | BindingFlags.Instance);
                    msgReceived.Invoke(s, new object[] { e.After });
                    //client.OnMessageReceived(e.After);
                }
            };

            //Convert our sync method to an async one and block the Main function until the bot disconnects
            client.ExecuteAndWait(async () =>
            {
                //Connect to the Discord server using our email and password
                await client.Connect(config["Email"], config["Password"]);
                Console.WriteLine($"Logged in as {client.CurrentUser.Name}");
                Console.WriteLine($"MH v. 0.3.0");

                //Use a channel whitelist
                client.Modules().Add(
                    new ChannelWhitelistModule(
                        Helpers.ConvertStringArrayToULongArray(
                            //config["API_testing"]
                            //config["LTT_general"],
                            //config["LTT_testing"],
                            config["FGO_playground"],
                            config["FGO_events"],
                            config["FGO_general"]
                        )
                    ),
                    nameof(ChannelWhitelistModule),
                    ModuleFilter.ChannelWhitelist
                );

                if (!client.Servers.Any())
                {
                    Console.WriteLine("Not a member of any server");
                }
                else
                {
                    foreach (var prChannel in client.PrivateChannels)
                    {
                        if (prChannel.Id == UInt64.Parse(config["PrivChat"]))
                        {
                            client.MessageReceived += (new Responder(prChannel, client).Respond);
                        }
                    }
                    foreach (var channel in Helpers.IterateChannels(client.Servers, printServerNames: true, printChannelNames: true))
                    {
                        if (!channel.IsPrivate && Helpers.IsWhilested(channel, client))
                        {
                            //Console.CancelKeyPress += async (s, e) => await client.SendMessage(channel, config["Goodbye"]);
                            client.MessageReceived += (new Responder(channel, client).Respond);
                            if (channel.Id != UInt64.Parse(config["API_testing"]))
                            {
                                if (Debugger.IsAttached)
                                {
                                   // await channel.SendMessage("MechHisui started in debug mode. Not all commands will be available.");
                                }
                                else
                                {
                                    await channel.SendMessage(config["Hello"]);
                                }
                            }
                        }
                    }
                    if (!Debugger.IsAttached)
                    {
                        client.AddNewHisuiBetsUsers(config);
                    }
                    Console.WriteLine($"Started up at {DateTime.Now}.");
                }
            });
        }
    }
}
