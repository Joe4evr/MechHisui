using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using Discord;
using Discord.WebSocket;
using SharedExtensions;

namespace GudakoBot
{
    class AprilFools
    {
        private static TimeZoneInfo JpnTimeZone => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time")
            : TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");

        private readonly Timer _aprilfools;
        private readonly Timer _rollback;

        public AprilFools(DiscordSocketClient client, ulong channel)
        {
            var utcNow = DateTime.UtcNow;
            var timeLeft = new DateTime(2018, 4, 1, 0, 0, 0, DateTimeKind.Utc) - utcNow
                + new DateTimeWithZone(utcNow, JpnTimeZone)
                    .TimeUntilNextLocalTimeAt(new TimeSpan(0, 0, 0));

            _aprilfools = new Timer(async _ =>
            {
                var self = client.CurrentUser;
                await self.ModifyAsync(u =>
                {
                    u.Username = "GudaoBot";
                    u.Avatar = new Image("GudaoAvatar.jpg");
                });

                if (client.GetChannel(channel) is ITextChannel ch)
                    await ch.SendMessageAsync("Good morning, director!");

            }, null, timeLeft, Timeout.InfiniteTimeSpan);

            _rollback = new Timer(_ =>
            {
                var self = client.CurrentUser;
                self.ModifyAsync(u =>
                {
                    u.Username = "GudakoBot";
                    u.Avatar = new Image("GudakoAvatar.jpg");
                });
            }, null, timeLeft.Add(TimeSpan.FromHours(24)), Timeout.InfiniteTimeSpan);
        }
    }
}
