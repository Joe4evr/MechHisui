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
        //static bool record = false;
        //static List<Channel> recChans = new List<Channel>();
        //static TextWriter recfile = null;

        public static void Main(string[] args)
        {
            var platform = PlatformServices.Create(PlatformServices.Default);

            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(platform.Application.ApplicationBasePath)
                .AddUserSecrets()
                .Build();

            var client = new DiscordClient();

            //Display all log messages in the console
            client.LogMessage += (s, e) => Console.WriteLine($"[{e.Severity}] {e.Source}: {e.Message}");

            //Echo back any message received, provided it didn't come from the bot itself
            //client.MessageReceived += async (s, e) =>
            //{
            //    if (IsWhilested(e.Channel, client))
            //    {
            //        if (record && recChans.Contains(e.Channel) && recfile != null)
            //        {
            //            await LogToFile(recfile, e.Message);
            //        }
            //        if (!e.Message.IsAuthor)
            //        {
            //            string response = String.Empty;
            //            var key = responseDict.Keys.Where(k => k.Contains(e.Message.Text)).SingleOrDefault();
            //            var sKey = spammableResponses.Keys.Where(k => k.Contains(e.Message.Text)).SingleOrDefault();
            //            if (key != null && responseDict.TryGetValue(key, out response) && response != String.Empty)
            //            {
            //                DateTime last;
            //                var msgTime = e.Message.Timestamp.ToUniversalTime();
            //                if (!lastResponses.TryGetValue(key, out last) || (DateTime.UtcNow - last) > TimeSpan.FromMinutes(1))
            //                {
            //                    lastResponses.AddOrUpdate(key, msgTime, (k, v) => v = msgTime);
            //                    await client.SendMessage(e.Channel, response);
            //                }
            //            }
            //            else if (sKey != null && spammableResponses.TryGetValue(sKey, out response))
            //            {
            //                await client.SendMessage(e.Channel, response);
            //            }
            //            if (e.Message.User == client.GetUser(e.Server, Int64.Parse(config["Owner"])))
            //            {
            //                switch (e.Message.Text)
            //                {
            //                    case ".disconnect":
            //                        if (record)
            //                            await EndRecord(client, e);
            //                        await client.SendMessage(e.Channel, "Mech-Hisui shutting down.");
            //                        client.Disconnect();
            //                        break;
            //                    case ".reset":
            //                        lastResponses = new ConcurrentDictionary<string[], DateTime>();
            //                        await client.SendMessage(e.Channel, "Timeouts reset.");
            //                        break;
            //                    case ".record":
            //                        if (record && recChans.Contains(e.Channel))
            //                        {
            //                            await client.SendMessage(e.Channel, "Already recording here.");
            //                        }
            //                        else
            //                        {
            //                            recfile = new StreamWriter($@"..\..\artifacts\obj\chatlogs\{e.Server.Name} - {e.Channel.Name} - {DateTime.UtcNow.Date}.txt");
            //                            recChans.Add(e.Channel);
            //                            record = true;
            //                            await client.SendMessage(e.Channel, $"Recording in {e.Channel}....");
            //                        }
            //                        break;
            //                    case ".endrecord":
            //                        if (record || !recChans.Contains(e.Channel))
            //                        {
            //                            await client.SendMessage(e.Channel, "Not recording here.");
            //                        }
            //                        else
            //                        {
            //                            await EndRecord(client, e);
            //                        }
            //                        break;
            //                    default:
            //                        break;
            //                }
            //            }
            //        }
            //    };

            //Add a ModuleService and CommandService
            client.AddService(new ModuleService());
            client.AddService(new CommandService(new CommandServiceConfig() { HelpMode = HelpMode.Public, CommandChar = '.' }));

            //register commands
            client.RegisterDisconnectCommand(config);
            client.RegisterResetCommand(config);
            //client.RegisterWikiCommand(config, new Wikier());


            //Convert our sync method to an async one and block the Main function until the bot disconnects
            client.Run(async () =>
            {
                //Connect to the Discord server using our email and password
                await client.Connect(config["Email"], config["Password"]);
                Console.WriteLine($"Logged in as {client.CurrentUserId}");
                
                //Use a channel whitelist
                client.Modules().Install(
                    new ChannelWhitelistModule(
                        Helpers.ConvertStringArrayToLongArray(
                            //config["API_testing"]
                            config["LTT_general"],
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
                    foreach (var server in client.AllServers)
                    {
                        Console.WriteLine(server.Name);
                        if (server.TextChannels.Any())
                        {
                            foreach (var channel in server.TextChannels)
                            {
                                Console.WriteLine($"{channel.Name}:  {channel.Id}");
                                if (!channel.IsPrivate && IsWhilested(channel, client))
                                {
                                    //Console.CancelKeyPress += async (s, e) => await client.SendMessage(channel, config["Goodbye"]);
                                    client.MessageReceived += (new Responder(channel, client).Respond);
                                    if (channel.Id != Int64.Parse(config["API_testing"]))
                                    {
                                        await client.SendMessage(channel, config["Hello"]);
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }

        //private static async Task LogToFile(TextWriter recfile, Message message)
        //{
        //    await recfile.WriteLineAsync($"{message.Timestamp.ToUniversalTime()} - {message.User.Name}\t\t: {message.Text}");
        //}

        //private static async Task EndRecord(DiscordClient client, MessageEventArgs e)
        //{
        //    record = false;
        //    await recfile.FlushAsync();
        //    recfile.Dispose();
        //    recChans.Remove(e.Channel);
        //    await client.SendMessage(e.Channel, $"Stopped recording in {e.Channel}.");
        //}

        internal static bool IsWhilested(Channel channel, DiscordClient client) => client.Modules().Modules
            .Where(m => m.Id == nameof(ChannelWhitelistModule).ToLowerInvariant())
            .SingleOrDefault()?
            .EnabledChannels
            .Contains(channel) ?? false;
    }
}
