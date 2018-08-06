using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using SharedExtensions;
using NodaTime;

namespace GudakoBot
{
    internal sealed class AprilFools
    {
        private readonly Func<LogMessage, Task> _logger;
        private readonly PeriodicMessageService _periodic;

        private readonly Timer _aprilfools;
        private readonly Timer _rollback;
        private readonly Timer _afmsgs;

        public AprilFools(
            DiscordSocketClient client,
            PeriodicMessageService periodic,
            ulong channel,
            Func<LogMessage, Task> logger = null)
        {
            _logger = logger ?? (m => Task.CompletedTask);
            _periodic = periodic;

            _aprilfools = new Timer(async _ =>
            {
                _periodic.StopTimer();

                await _logger(new LogMessage(LogSeverity.Info, "AF", "Enabling April Fool's service"));

                var self = client.CurrentUser;
                await self.ModifyAsync(u =>
                {
                    u.Username = "GudaoBot";
                    u.Avatar = new Image("GudaoAvatar.jpg");
                });

                if (client.GetChannel(channel) is ITextChannel ch)
                    await ch.SendMessageAsync("Good morning, director!");

                _afmsgs.Change(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));

            }, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            _rollback = new Timer(async _ =>
            {
                _afmsgs.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

                await _logger(new LogMessage(LogSeverity.Info, "AF", "Enabling April Fool's service"));

                var self = client.CurrentUser;
                await self.ModifyAsync(u =>
                {
                    u.Username = "GudakoBot";
                    u.Avatar = new Image("GudakoAvatar.jpg");
                });

                _periodic.StartTimer();
            }, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            _afmsgs = new Timer(async _ =>
            {
                if (client.GetChannel(channel) is ITextChannel ch)
                    await ch.SendMessageAsync("Good morning, director!");


            }, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            client.Ready += () =>
            {
                var jpnnow = Instant.FromDateTimeOffset(DateTimeOffset.UtcNow).InZone(NodaTimeExtensions.JpnTimeZone);
                var aprilFirst = new AnnualDate(month: 4, day: 1);
                var (timeToStart, timeToStop) = (jpnnow.IsAnnualDate(aprilFirst))
                    ? (TimeSpan.Zero, jpnnow.TimeUntilNextOccurrance(LocalTime.Midnight).ToTimeSpan())
                    : (jpnnow.TimeUntilNextOccurrance(aprilFirst).ToTimeSpan(), (TimeSpan?)null);

                _aprilfools.Change(timeToStart, Timeout.InfiniteTimeSpan);
                _rollback.Change(timeToStop ?? timeToStart.Add(TimeSpan.FromHours(24)), Timeout.InfiniteTimeSpan);

                return Task.CompletedTask;
            };
        }
    }
}
