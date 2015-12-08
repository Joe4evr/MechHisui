using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.PlatformAbstractions;
using Discord;
using Discord.Commands;
using Discord.Modules;
using MechHisui.Commands;
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

            var client = new DiscordClient();

            Console.CancelKeyPress += async (s,e) => await RegisterCommands.Disconnect(client, config);

            //Display all log messages in the console
            client.LogMessage += (s, e) => Console.WriteLine($"[{e.Severity}] {e.Source}: {e.Message}");
            
            //Add a ModuleService and CommandService
            client.AddService(new ModuleService());
            client.AddService(new CommandService(new CommandServiceConfig() { HelpMode = HelpMode.Public, CommandChar = '.' }));

            //register commands
            Console.WriteLine("Registering 'Add alias'...");
            client.RegisterAddAliasCommand(config);
            Console.WriteLine("Registering 'Add channel'...");
            client.RegisterAddChannelCommand(config);
            Console.WriteLine("Registering 'Daily'...");
            client.RegisterDailyCommand(config);
            Console.WriteLine("Registering 'Disconnect'...");
            client.RegisterDisconnectCommand(config);
            Console.WriteLine("Registering 'Friends'...");
            client.RegisterFriendsCommand(config);
            Console.WriteLine("Registering 'Info'...");
            client.RegisterInfoCommand(config);
            Console.WriteLine("Registering 'Known'...");
            client.RegisterKnownChannelsCommand(config);
            Console.WriteLine("Registering 'Learn'...");
            client.RegisterLearnCommand(config);
            Console.WriteLine("Registering 'Mark'...");
            client.RegisterMarkCommand(config);
            Console.WriteLine("Registering 'Reset'...");
            client.RegisterResetCommand(config);
            //Console.WriteLine("Registering 'Trivia'...");
            //client.RegisterTriviaCommand(config);
            Console.WriteLine("Registering 'Stats'...");
            var statService = new StatService(config);
            client.RegisterStatsCommand(config, statService);
            Console.WriteLine("Registering 'Update'...");
            client.RegisterUpdateCommand(config, statService);
            Console.WriteLine("Registering 'Where'...");
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
                    //foreach (var server in client.AllServers)
                    //{
                    //    Console.WriteLine(server.Name);
                    //    if (server.TextChannels.Any())
                    //    {
                    //        foreach (var channel in server.TextChannels)
                    //        {
                    //            Console.WriteLine($"{channel.Name}:  {channel.Id}");
                    //            if (!channel.IsPrivate && Helpers.IsWhilested(channel, client))
                    //            {
                    //                //Console.CancelKeyPress += async (s, e) => await client.SendMessage(channel, config["Goodbye"]);
                    //                client.MessageReceived += (new Responder(channel, client).Respond);
                    //                if (channel.Id != Int64.Parse(config["API_testing"]))
                    //                {
                    //                    await client.SendMessage(channel, config["Hello"]);
                    //                }
                    //            }
                    //        }
                    //    }
                    //}
                }
            });
        }
    }
}
