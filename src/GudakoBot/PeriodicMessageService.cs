using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace GudakoBot
{
    public class PeriodicMessageService
    {
        private readonly HashSet<ulong> _channels;
        private readonly Timer _timer;
        private readonly Random _rng = new Random();

        private string lastLine;

        public PeriodicMessageService(
            DiscordSocketClient client,
            HashSet<ulong> channels,
            IEnumerable<string> lines)
        {
            _channels = channels ?? new HashSet<ulong>();

            _timer = new Timer(async s =>
            {
                var chs = _channels.Select(id => client.GetChannel(id) as ITextChannel);

                string str;
                lines = lines.Shuffle();

                do str = lines.ElementAt(_rng.Next(maxValue: lines.Count()));
                while (str == lastLine);

                Console.WriteLine($"{DateTime.Now}: Sending messages.");

                var tasks = new List<Task>();
                foreach (var ch in chs)
                {
                    if (ch == null)
                    {
                        Console.WriteLine($"{DateTime.Now,-19} Channel couldn't be found. Waiting for next interval.");
                    }
                    else
                    {
                        tasks.Add(ch.SendMessageAsync(str));
                    }
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);
                lastLine = str;
            }, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(30));
        }
    }
}
