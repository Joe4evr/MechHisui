using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Addons.SimpleConfig;
using Discord.Addons.WS4NetCompatibility;
using System.Diagnostics;

namespace GudakoBot
{
    public class Program
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
            Console.WriteLine($"Loaded {config.Lines.Count()} lines.");

            var client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Warning,
                WebSocketProvider = () => new WS4NetProvider()
            });

            //Display all log messages in the console
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

            client.MessageReceived += async msg =>
            {
                if (msg.Author.Id == config.OwnerId && msg.Content == "-new")
                {
                    Console.WriteLine($"{DateTime.Now}: Reloading lines");
                    config = store.Load();
                    await msg.Channel.SendMessageAsync(config.Lines.Last());
                    timer.Change(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
                }
            };

            client.Ready += () =>
            {
                Console.WriteLine($"Logged in as {client.CurrentUser.Username}");
                Console.WriteLine($"Started up at {DateTime.Now}.");

                var rng = new Random();
                timer = new Timer(async s =>
                {
                    var channel = client.GetChannel(config.FgoGeneral) as ITextChannel;
                    if (channel == null)
                    {
                        Console.WriteLine($"Channel was null. Waiting for next interval.");
                    }
                    else
                    {
                        string str;
                        config.Lines = config.Lines.Shuffle();

                        do str = config.Lines.ElementAt(rng.Next(maxValue: config.Lines.Count()));
                        while (str == lastLine);

                        Console.WriteLine($"{DateTime.Now}: Sending message.");
                        await channel.SendMessageAsync(str);
                        lastLine = str;
                    }
                },
                null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(30));
                return Task.CompletedTask;
            };

            await client.LoginAsync(TokenType.Bot, config.LoginToken);
            await client.ConnectAsync(waitForGuilds: true);
            await Task.Delay(-1);
        }

        private static readonly IConfigStore<GudakoConfig> store = new JsonConfigStore<GudakoConfig>(
            Debugger.IsAttached
            ? "config.json"
            : "../GudakoBot-jsons/config.json");
        private static Timer timer;
        private static string lastLine;
    }

    static class Ext
    {
        //Method for randomizing lists using a Fisher-Yates shuffle.
        //Taken from http://stackoverflow.com/questions/273313/
        /// <summary>
        /// Perform a Fisher-Yates shuffle on a collection implementing <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="source">The list to shuffle.</param>
        /// <remarks>Adapted from http://stackoverflow.com/questions/273313/. </remarks>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            var provider =
#if NET46
                new RNGCryptoServiceProvider();
#else
                RandomNumberGenerator.Create();
#endif
            var buffer = source.ToList();
            int n = buffer.Count;
            while (n > 1)
            {
                byte[] box = new byte[(n / Byte.MaxValue) + 1];
                int boxSum;
                do
                {
                    provider.GetBytes(box);
                    boxSum = box.Sum(b => b);
                }
                while (!(boxSum < n * ((Byte.MaxValue * box.Length) / n)));
                int k = (boxSum % n);
                n--;
                T value = buffer[k];
                buffer[k] = buffer[n];
                buffer[n] = value;
            }

            return buffer;
        }
    }
}
