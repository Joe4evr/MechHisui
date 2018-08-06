using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using SharedExtensions;

namespace GudakoBot
{
    public sealed class PeriodicMessageService
    {
        private readonly Func<LogMessage, Task> _logger;
        private readonly Timer _timer;
        private readonly Random _rng = new Random();

        private string _lastLine;

        internal IEnumerable<string> Lines { private get; set; }

        public PeriodicMessageService(
            DiscordSocketClient client,
            ulong channel,
            IEnumerable<string> lines,
            Func<LogMessage, Task> logger = null)
        {
            _logger = logger ?? (m => Task.CompletedTask);
            Lines = lines;
            _timer = new Timer(async s =>
            {

                string str;
                Lines = Lines.Shuffle(7);

                do str = Lines.ElementAt(_rng.Next(maxValue: lines.Count()));
                while (str == _lastLine);

                if (client.GetChannel(channel) is ITextChannel ch)
                    await ch.SendMessageAsync(str).ConfigureAwait(false);
                else
                    await _logger(new LogMessage(LogSeverity.Info, "Periodic", "Channel couldn't be found. Waiting for next interval.")).ConfigureAwait(false);
                
                _lastLine = str;
            }, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            client.Ready += () =>
            {
                _timer.Change(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(60));
                return Task.CompletedTask;
            };
        }

        internal void StopTimer() => _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        internal void StartTimer() => _timer.Change(TimeSpan.FromMinutes(60), TimeSpan.FromMinutes(60));
    }
}
