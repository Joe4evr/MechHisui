using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Addons.SimpleConfig;
using Discord.Addons.SimplePermissions;
using Discord.Addons.WS4NetCompatibility;
using Discord.Commands;
using MechHisui.Core;
//using MechHisui.Core.Modules;
using MechHisui.FateGOLib;
using MechHisui.HisuiBets;
using MechHisui.SecretHitler;

namespace MechHisui
{
    public class Program
    {
        public static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    AsyncMain(args).GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{DateTime.Now}: {e.Message}\n{e.StackTrace}");
                    Console.ReadLine();
                }
            }

            //client.MessageUpdated += async (s, e) =>
            //{
            //    if (!(await e.Channel.DownloadMessages(10, e.Before.Id, Relative.After)).Any(m => m.IsAuthor))
            //    {
            //        var msgReceived = typeof(DiscordClient).GetMethod("OnMessageReceived", BindingFlags.NonPublic | BindingFlags.Instance);
            //        msgReceived.Invoke(s, new object[] { e.After });
            //        //client.OnMessageReceived(e.After);
            //    }
            //};
            //try
            //{
            //    //Convert our sync method to an async one and block the Main function until the bot disconnects
            //    client.ExecuteAndWait(async () =>
            //    {
            //        await client.Connect(config["LoginToken"]);
            //        Console.WriteLine($"Logged in as {client.CurrentUser.Name}");
            //        Console.WriteLine($"MH v. 0.3.0");
            //        await Task.Delay(3000);
            //        if (!client.Servers.Any())
            //        {
            //            Console.WriteLine("Not a member of any server");
            //        }
            //        else
            //        {
            //            foreach (var channel in Helpers.IterateChannels(client.Servers, printServerNames: true, printChannelNames: true))
            //            {
            //                if (!channel.IsPrivate && Helpers.IsWhilested(channel, client))
            //                {
            //                    //Console.CancelKeyPress += async (s, e) => await client.SendMessage(channel, config["Goodbye"]);
            //                    if (channel.Id != UInt64.Parse(config["API_testing"]))
            //                    {
            //                        if (Debugger.IsAttached)
            //                        {
            //                            // await channel.SendMessage("MechHisui started in debug mode. Not all commands will be available.");
            //                        }
            //                        else if (lastcode != -1 && channel.Id != UInt64.Parse(config["FGO_events"]))
            //                        {
            //                            await channel.SendMessage(config["Hello"]);
            //                        }
            //                    }
            //                }
            //            }
            //            client.GetModule<HisuiBetsModule>().Instance.AddNewHisuiBetsUsers(client, config);
            //            Console.WriteLine($"Started up at {DateTime.Now}.");
            //        }
            //    });
            //}
            //catch (Exception ex)
            //{
            //    File.AppendAllText(Path.Combine(config["Logs"], "crashlogs.txt"), $"{DateTime.Now} - {ex.Message}\n{ex.StackTrace}\n");
            //    Environment.Exit(-1);
            //}
        }

        private static async Task AsyncMain(string[] args)
        {
            var client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                WebSocketProvider = () => new WS4NetProvider()
            });

            client.Log += msg =>
            {
                var cc = Console.ForegroundColor;
                switch (msg.Severity)
                {
                    case LogSeverity.Critical:
                    case LogSeverity.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case LogSeverity.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LogSeverity.Info:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case LogSeverity.Verbose:
                    case LogSeverity.Debug:
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        break;
                }
                Console.WriteLine($"{DateTime.Now,-19} [{msg.Severity,8}] {msg.Source}: {msg.Message}");
                Console.ForegroundColor = cc;
                return Task.CompletedTask;
            };
            client.Ready += () =>
            {
                Console.WriteLine($"Logged in as {client.CurrentUser.Username}");
                Console.WriteLine($"Started up at {DateTime.Now}.");


                return Task.CompletedTask;
            };
            await InitCommands(client);

            await client.LoginAsync(TokenType.Bot, store.Load().LoginToken);
            await client.ConnectAsync(waitForGuilds: true);
            await Task.Delay(-1);
        }

        private static async Task InitCommands(DiscordSocketClient client)
        {
            var temp = store.Load();
            var fgo = new StatService(temp.FgoConfig);

            depmap.Add(depmap);
            depmap.Add(commands);
            depmap.Add(client);
            //depmap.Add<IConfigStore<IPermissionConfig>>(store);
            depmap.Add(new PermissionsService(store, client));
            depmap.Add(new HisuiBankService(client, new BankOfHisui(store.Load().BankPath)));
            depmap.Add(new SecretHitlerService(store.Load().SHConfigs));
            depmap.Add<ISelfUser>(client.CurrentUser);

            //commands.AddTypeReader<DiceRoll>(new DiceTypeReader());
            //await commands.AddModule<DiceRollModule>();

            //await commands.AddModule<PermissionsModule>();
            //await commands.InitFgoModules(fgo);
            //await commands.AddModule<HisuiBankModule>();
            //await commands.AddModule<HisuiBetsModule>();
            await commands.AddModule<SecretHitlerModule>();
            //await commands.AddModules(Assembly.GetEntryAssembly(), depmap);

            client.MessageReceived += HandleCommand;
        }

        private static async Task HandleCommand(SocketMessage arg)
        {
            var msg = arg as IUserMessage;
            if (msg == null) return;

            int pos = 0;
            var user = arg.Discord.CurrentUser;
            if (msg.HasCharPrefix('.', ref pos) || msg.HasMentionPrefix(user, ref pos))
            {
                var context = new CommandContext(arg.Discord, msg);
                var result = await commands.Execute(context, pos, dependencyMap: depmap);

                //if (!result.IsSuccess)
                //    await msg.Channel.SendMessageAsync(result.ErrorReason);
            }
        }

        private static readonly IConfigStore<MechHisuiConfig> store = new JsonConfigStore<MechHisuiConfig>("../MechHisui-jsons/config.json");
        private static readonly IDependencyMap depmap = new DependencyMap();
        private static readonly CommandService commands = new CommandService();
    }
}
