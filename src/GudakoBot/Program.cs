using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.PlatformAbstractions;
using Discord;
using Newtonsoft.Json;

using static JiiLib.Extensions;

namespace GudakoBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            PlatformServices ps = PlatformServices.Default;
            IApplicationEnvironment env = ps.Application;
            //Console.WriteLine($"Base: {env.ApplicationBasePath}");

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
                builder.AddJsonFile(@"..\..\..\..\GudakoBot-jsons\secrets.json");
            }

            IConfiguration config = builder.Build();

            Console.WriteLine("Loading chat lines...");
            LoadLines(config);
            Console.WriteLine($"Loaded {Randomlines.Count} lines.");

            var client = new DiscordClient(
                new DiscordConfig
                {
                    AppName = "GudakoBot",
                    CacheToken = true,
                    LogLevel = LogSeverity.Warning
                });

            //Display all log messages in the console
            client.Log.Message += (s, e) => Console.WriteLine($"[{e.Severity}] {e.Source}: {e.Message}");

            client.ExecuteAndWait(async () =>
            {
                //Connect to the Discord server using our email and password
                await client.Connect(config["Email"], config["Password"]);
                //client.Token = (await client.Send(new LoginRequest { Email = config["Email"], Password = config["Password"] })).Token;
                Console.WriteLine($"Logged in as {client.CurrentUser.Name}");
                Console.WriteLine($"Started up at {DateTime.Now}.");

                var rng = new Random();
                timer = new Timer(async s =>
                {
                    LoadLines(config);
                    Console.WriteLine($"{DateTime.Now}: Sending message.");
                    Randomlines.Shuffle();
                    await client.GetChannel(UInt64.Parse(config["FGO_general"]))
                        .SendMessage(Randomlines.ElementAt(rng.Next(maxValue: Randomlines.Count)));
                },
                null,
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(30));
            });
        }

        private static void LoadLines(IConfiguration config)
        {
            using (TextReader tr = new StreamReader(new FileStream(config["LinesPath"], FileMode.Open, FileAccess.Read)))
            {
                Randomlines = JsonConvert.DeserializeObject<List<string>>(tr.ReadToEnd());
            }
        }

        private static Timer timer;

        private static List<string> Randomlines = new List<string>();
    }
}
