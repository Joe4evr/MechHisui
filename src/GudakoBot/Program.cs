using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.PlatformAbstractions;
using Discord;
using Newtonsoft.Json;

namespace GudakoBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            PlatformServices ps = PlatformServices.Default;
            var env = ps.Application;
            Console.WriteLine($"Base: {env.ApplicationBasePath}");

            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .SetBasePath(env.ApplicationBasePath);

            IHostingEnvironment hostingEnv = new HostingEnvironment();
            hostingEnv.Initialize(env.ApplicationName, env.ApplicationBasePath, new WebHostOptions());

            Console.WriteLine("Loading from jsons directory");
            builder.AddInMemoryCollection(JsonConvert.DeserializeObject<Dictionary<string, string>>(
                File.ReadAllText(@"C:\PublishOutputs\GudakoBot-jsons\secrets.json")
            ));

            IConfiguration config = builder.Build();

            ulong owner = UInt64.Parse(config["Owner"]);
            ulong fgogen = UInt64.Parse(config["FGO_general"]);

            Console.WriteLine("Loading chat lines...");
            LoadLines(config);
            Console.WriteLine($"Loaded {Randomlines.Count()} lines.");

            var client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Warning
            });

            //Display all log messages in the console
            client.Log += async msg => await Task.Run(() => Console.WriteLine($"[{msg.Severity}] {msg.Source}: {msg.Message}")); //File.AppendAllText(logPath, $"[{e.Severity}] {e.Source}: {e.Message}"); 
            client.MessageReceived += async msg =>
            {
                if (msg.Author.Id == owner && msg.Content == "-new")
                {
                    Console.WriteLine($"{DateTime.Now}: Reloading lines");
                    LoadLines(config);
                    await msg.Channel.SendMessageAsync(Randomlines.Last());
                    timer.Change(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
                }
            };
            client.Ready += async () =>
            {
                Console.WriteLine($"Logged in as {(await client.GetCurrentUserAsync()).Username}");
                Console.WriteLine($"Started up at {DateTime.Now}.");

                var rng = new Random();
                timer = new Timer(async s =>
                {
                    var channel = await client.GetChannelAsync(fgogen) as ITextChannel;
                    if (channel == null)
                    {
                        Console.WriteLine($"Channel was null. Waiting for next interval.");
                    }
                    else
                    {
                        string str;
                        Randomlines = Randomlines.Shuffle();

                        do str = Randomlines.ElementAt(rng.Next(maxValue: Randomlines.Count()));
                        while (str == lastLine);

                        Console.WriteLine($"{DateTime.Now}: Sending message.");
                        await channel.SendMessageAsync(str);
                        lastLine = str;
                    }
                },
                null,
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(30)
                );
            };

            Task.Run(async () =>
            {
                await client.LoginAsync(TokenType.Bot, config["LoginToken"]);
                await client.ConnectAsync(true);
            }).GetAwaiter().GetResult();
        }

        private static void LoadLines(IConfiguration config)
        {
            using (TextReader tr = new StreamReader(new FileStream(config["LinesPath"], FileMode.Open, FileAccess.Read)))
            {
                Randomlines = JsonConvert.DeserializeObject<List<string>>(tr.ReadToEnd());
            }
        }

        private static IEnumerable<string> Randomlines = new List<string>();
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
