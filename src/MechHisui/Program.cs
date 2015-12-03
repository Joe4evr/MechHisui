using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.PlatformAbstractions;
using Discord;
using Discord.Commands;
using Discord.Modules;

namespace MechHisui
{
    public partial class Program
    {
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
            client.MessageReceived += async (s, e) =>
            {
                if (IsWhilested(e.Channel, client) && !e.Message.IsAuthor)
                {
                    await Task.Delay(50);
                    //await client.SendMessage(e.Channel, e.Message.Text);
                    string response = string.Empty;
                    var keys = responseDict.Keys.Where(k => k.Contains(e.Message.Text));
                    var key = keys.SingleOrDefault();
                    if (key != null && responseDict.TryGetValue(key, out response) && response != string.Empty)
                    {
                        DateTime last;
                        var msgTime = e.Message.Timestamp.ToUniversalTime();
                        if (!lastResponses.TryGetValue(key, out last) || (DateTime.UtcNow - last) > TimeSpan.FromMinutes(1))
                        {
                            lastResponses.AddOrUpdate(key, msgTime, (k,v) => v = msgTime);
                            await client.SendMessage(e.Channel, response);
                        }
                    }
                    else if (spammableResponses.Keys.Where(k => k.Contains(e.Message.Text)).SingleOrDefault() != null && spammableResponses.TryGetValue(key, out response))
                    {
                        await client.SendMessage(e.Channel, response);
                    }
                }
            };

            //client.Commands().RanCommand += async (s, e) =>
            //{
            //    await Task.Delay(50);
            //    client.Commands().
            //};

            //Add a ModuleService
            client.AddService(new ModuleService());

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
                        foreach (var channel in server.TextChannels)
                        {
                            if (IsWhilested(channel, client))
                            {
                                await client.SendMessage(channel, "Connected");
                            }
                        }
                    }
                }
            });
        }

        private static bool IsWhilested(Channel channel, DiscordClient client) => client.Modules().Modules
            .Where(m => (m.FilterType & FilterType.ChannelWhitelist) == FilterType.ChannelWhitelist)
            .Single()
            .EnabledChannels
            .Contains(channel);
    }
}
