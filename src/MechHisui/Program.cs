using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Discord;
using Discord.Commands;
using Discord.Modules;
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

            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(platform.Application.ApplicationBasePath)
                .AddUserSecrets()
                .Build();

            JiiLib.Net.IJsonApiService apiService = new JiiLib.Net.GoogleScriptApiService(
                Path.Combine(config["Secrets_Path"], "client_secret.json"),
                Path.Combine(config["Secrets_Path"], "scriptcreds"),
                config["Project_Key"],
                "MechHisui",
                "exportServants",
                new string[] { "https://www.googleapis.com/auth/spreadsheets.readonly" });

            StatService statService = new StatService(apiService, config["ServantAliasPath"]);

            //IServiceCollection services = new ServiceCollection()
            //    .AddInstance(config)
            //    .AddInstance(new StatService(apiService, config["ServantAliasPath"]));

            //var provider = services.BuildServiceProvider();

            var client = new DiscordClient();

            Console.CancelKeyPress += async (s,e) => await RegisterCommands.Disconnect(client, config);

            //Display all log messages in the console
            client.LogMessage += (s, e) => Console.WriteLine($"[{e.Severity}] {e.Source}: {e.Message}");
            
            //Add a ModuleService and CommandService
            client.AddService(new ModuleService());
            client.AddService(new CommandService(new CommandServiceConfig() { HelpMode = HelpMode.Public, CommandChar = '.' }));

            //register commands
            client.RegisterAddChannelCommand(config);
            client.RegisterDailyCommand(config);
            client.RegisterDisconnectCommand(config);
            client.RegisterFriendsCommand(config);
            client.RegisterInfoCommand(config);
            client.RegisterKnownChannelsCommand(config);
            client.RegisterLearnCommand(config);
            client.RegisterMarkCommand(config);
            //client.RegisterRecordingCommand(config);
            client.RegisterResetCommand(config);
            //client.RegisterTriviaCommand(config);
            client.RegisterStatsCommand(config, statService);
            client.RegisterQuartzCommand(config);
            client.RegisterWhereCommand(config);

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
                    foreach (var channel in Helpers.IterateChannels(client.AllServers, printServerNames: true, printChannelNames: true))
                    {
                        if (!channel.IsPrivate && Helpers.IsWhilested(channel, client))
                        {
                            //Console.CancelKeyPress += async (s, e) => await client.SendMessage(channel, config["Goodbye"]);
                            client.MessageReceived += (new Responder(channel, client).Respond);
                            if (channel.Id != long.Parse(config["API_testing"]))
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
