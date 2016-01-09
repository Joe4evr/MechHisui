﻿using System;
using System.Linq;
using Microsoft.AspNet.Hosting;
using Microsoft.Dnx.Compilation;
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
            ILibraryExporter exporter = CompilationServices.Default.LibraryExporter;
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

            var client = new DiscordClient(new DiscordClientConfig
            {
                LogLevel = LogMessageSeverity.Warning
            });

            client.Disconnected += (s, e) => Environment.Exit(0);
            Console.CancelKeyPress += async (s, e) => await RegisterCommands.Disconnect(client, config);

            //Display all log messages in the console
            client.LogMessage += (s, e) => Console.WriteLine($"[{e.Severity}] {e.Source}: {e.Message}");

            //Add a ModuleService and CommandService
            client.AddService(new ModuleService());
            client.AddService(new CommandService(new CommandServiceConfig { HelpMode = HelpMode.Public, CommandChar = '.' }));

            //register commands
            client.RegisterAddChannelCommand(config);
            client.RegisterDeleteCommand(config);
            client.RegisterDisconnectCommand(config);
            client.RegisterEvalCommand(config);
            client.RegisterInfoCommand(config);
            client.RegisterKnownChannelsCommand(config);
            client.RegisterLearnCommand(config);
            client.RegisterMarkCommand(config);
            //client.RegisterRecordingCommand(config);
            client.RegisterResetCommand(config);
            client.RegisterThemeCommand(config);
            client.RegisterWhereCommand(config);
            client.RegisterXmasCommand(config);

            client.RegisterDailyCommand(config);
            client.RegisterEventCommand(config);
            client.RegisterFriendsCommand(config);
            client.RegisterLoginBonusCommand(config);
            client.RegisterStatsCommands(config);
            client.RegisterQuartzCommand(config);
            client.RegisterZoukenCommand(config);

            client.RegisterTriviaCommand(config);

            Responses.InitResponses(config);


            //Convert our sync method to an async one and block the Main function until the bot disconnects
            client.Run(async () =>
            {
                //Connect to the Discord server using our email and password
                await client.Connect(config["Email"], config["Password"]);
                var server = client.AllServers.FirstOrDefault();
                if (server != null)
                {
                    Console.WriteLine($"Logged in as {client.GetUser(server, client.CurrentUserId).Name}");
                }

                //Use a channel whitelist
                client.Modules().Install(
                    new ChannelWhitelistModule(
                        Helpers.ConvertStringArrayToLongArray(
                            //config["API_testing"]
                            //config["LTT_general"],
                            //config["LTT_testing"],
                            config["FGO_trivia"],
                            config["FGO_general"]
                        )
                    ),
                    nameof(ChannelWhitelistModule),
                    FilterType.ChannelWhitelist
                );

                if (!client.AllServers.Any())
                {
                    Console.WriteLine("Not a member of any server");
                }
                else
                {
                    foreach (var prChannel in client.PrivateChannels)
                    {
                        if (prChannel.Id == Int64.Parse(config["PrivChat"]))
                        {
                            client.MessageReceived += (new Responder(prChannel, client).Respond);
                        }
                    }
                    foreach (var channel in Helpers.IterateChannels(client.AllServers, printServerNames: true, printChannelNames: true))
                    {
                        if (!channel.IsPrivate && Helpers.IsWhilested(channel, client))
                        {
                            //Console.CancelKeyPress += async (s, e) => await client.SendMessage(channel, config["Goodbye"]);
                            client.MessageReceived += (new Responder(channel, client).Respond);
                            if (channel.Id != long.Parse(config["API_testing"]) && channel.Id != Int64.Parse(config["FGO_trivia"]))
                            {
                                await client.SendMessage(channel, config["Hello"]);
                            }
                        }
                    }
                    Console.WriteLine($"Started up at {DateTime.Now}.");
                }
            });
        }
    }
}
