using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
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
            var env = ps.Application;
            Console.WriteLine($"Base: {env.ApplicationBasePath}");

            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .SetBasePath(env.ApplicationBasePath);

            IHostingEnvironment hostingEnv = new HostingEnvironment();
            hostingEnv.Initialize(env.ApplicationName, env.ApplicationBasePath, new WebHostOptions());
            if (hostingEnv.IsDevelopment())
            {
                Console.WriteLine("Loading from UserSecret store");
                builder.AddUserSecrets();
            }
            else
            {
                Console.WriteLine("Loading from jsons directory");
                builder.AddInMemoryCollection(JsonConvert.DeserializeObject<Dictionary<string, string>>(
                    File.ReadAllText(@"../GudakoBot-jsons/secrets.json")
                ));
            }

            IConfiguration config = builder.Build();

            ulong owner = UInt64.Parse(config["Owner"]);
            ulong fgogen = UInt64.Parse(config["FGO_general"]);

            Console.WriteLine("Loading chat lines...");
            LoadLines(config);
            Console.WriteLine($"Loaded {Randomlines.Count()} lines.");

            var client = new DiscordClient(conf =>
            {
                conf.AppName = "GudakoBot";
                conf.CacheToken = true;
                conf.LogLevel = /*Debugger.IsAttached ? LogSeverity.Verbose :*/ LogSeverity.Warning;
                //conf.UseLargeThreshold = true;
            });

            //Display all log messages in the console
            client.Log.Message += (s, e) => Console.WriteLine($"[{e.Severity}] {e.Source}: {e.Message}"); //File.AppendAllText(logPath, $"[{e.Severity}] {e.Source}: {e.Message}"); 
            client.MessageReceived += async (s, e) =>
            {
                if (e.Message.User.Id == owner && e.Message.Text == "-new")
                {
                    Console.WriteLine($"{DateTime.Now}: Reloading lines");
                    LoadLines(config);
                    await e.Channel.SendWithRetry(Randomlines.Last());
                    timer.Change(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
                }
            };


            client.ExecuteAndWait(async () =>
            {
                //Connect to the Discord server using our email and password
                await client.Connect(config["LoginToken"]);
                //client.Token = (await client.Send(new LoginRequest { Email = config["Email"], Password = config["Password"] })).Token;
                Console.WriteLine($"Logged in as {client.CurrentUser.Name}");
                Console.WriteLine($"Started up at {DateTime.Now}.");

                var rng = new Random();
                timer = new Timer(async s =>
                {
                    var channel = client.GetChannel(fgogen);
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
                        await channel.SendWithRetry(str);
                        lastLine = str;
                    }
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

        private static IEnumerable<string> Randomlines = new List<string>();
        private static Timer timer;
        private static string lastLine;
    }
}
