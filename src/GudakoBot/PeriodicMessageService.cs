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
        private readonly Func<LogMessage, Task> _logger;
        private readonly Timer _timer;
        private readonly Random _rng = new Random();

        private string _lastLine;

        public PeriodicMessageService(
            DiscordSocketClient client,
            ulong channel,
            IEnumerable<string> lines,
            Func<LogMessage, Task> logger = null)
        {
            _logger = logger ?? (m => Task.CompletedTask);

            _timer = new Timer(async s =>
            {

                string str;
                lines = lines.Shuffle();

                do str = lines.ElementAt(_rng.Next(maxValue: lines.Count()));
                while (str == _lastLine);

                var ch = client.GetChannel(channel) as ITextChannel;
                if (ch == null)
                {
                    await _logger(new LogMessage(LogSeverity.Info, "Periodic", $"{DateTime.Now,-19} Channel couldn't be found. Waiting for next interval.")).ConfigureAwait(false);
                }
                else
                {
                    await ch.SendMessageAsync(str).ConfigureAwait(false);
                }
                
                _lastLine = str;
            }, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(30));
        }
    }
}
