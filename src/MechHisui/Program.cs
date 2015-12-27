﻿using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.PlatformAbstractions;
using Discord;
using Discord.Commands;
using Discord.Modules;
using JiiLib.Net;
using MechHisui.Commands;
using MechHisui.FateGOLib;
using MechHisui.Modules;

namespace MechHisui
{
    public static partial class Program
    {
        public static void Main(string[] args)
        {
            var platform = PlatformServices.Create(PlatformServices.Default);

            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(platform.Application.ApplicationBasePath);

            if (args.Contains("--environment") && args[Array.IndexOf(args, "--environment") + 1] == "Development")
            {
                Console.WriteLine("Loading from UserSecret store");
                builder.AddUserSecrets();
            }
            else
            {
                Console.WriteLine("Loading from jsons directory");
                builder.AddJsonFile(@"..\..\..\..\MechHisui-jsons\secrets.json");
            }

            IConfiguration config = builder.Build();

            GoogleScriptApiService apiService = new GoogleScriptApiService(
                Path.Combine(config["Secrets_Path"], "client_secret.json"),
                Path.Combine(config["Secrets_Path"], "scriptcreds"),
                "MechHisui",
                config["Project_Key"],
                "exportSheet",
                new string[] { "https://www.googleapis.com/auth/spreadsheets.readonly" });


            StatService statService = new StatService(apiService,
                Path.Combine(config["AliasPath"], "servants.json"),
                Path.Combine(config["AliasPath"], "ces.json"),
                Path.Combine(config["AliasPath"], "mystic.json"));
            try
            {
                statService.UpdateProfileListsAsync().Wait();
                statService.UpdateEventListAsync().Wait();
                statService.UpdateMysticCodesListAsync().Wait();
            }
            catch (Exception)
            {
                Environment.Exit(0);
            }

            var client = new DiscordClient();

            client.Disconnected += (s, e) => Environment.Exit(0);
            Console.CancelKeyPress += async (s, e) => await RegisterCommands.Disconnect(client, config);

            //Display all log messages in the console
            client.LogMessage += (s, e) => Console.WriteLine($"[{e.Severity}] {e.Source}: {e.Message}");

            //Add a ModuleService and CommandService
            client.AddService(new ModuleService());
            client.AddService(new CommandService(new CommandServiceConfig() { HelpMode = HelpMode.Public, CommandChar = '.' }));

            //register commands
            client.RegisterAddChannelCommand(config);
            client.RegisterDisconnectCommand(config);
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
            client.RegisterStatsCommand(config, statService);
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
                }
            });
        }
    }
}
