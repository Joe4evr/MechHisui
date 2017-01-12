using System;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.SimplePermissions;
using Discord.Net.Providers.WS4Net;
using Discord.WebSocket;

namespace Kohaku
{
    class Program
    {
        static void Main(string[] args)
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
        }

        static async Task AsyncMain(string[] args)
        {
            Console.WriteLine("Loading config...");
            var config = store.Load();

            var client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Warning,
                WebSocketProvider = WS4NetProvider.Instance
            });

            await client.LoginAsync(TokenType.Bot, config.LoginToken);
            await client.ConnectAsync(waitForGuilds: true);
            await Task.Delay(-1);
        }

        private static JsonConfigStore<KohakuConfig> store = new JsonConfigStore<KohakuConfig>("config.json");
    }
}