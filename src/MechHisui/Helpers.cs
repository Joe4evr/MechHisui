using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Modules;
using MechHisui.Modules;

namespace MechHisui
{
    internal static class Helpers
    {
        internal static bool IsWhilested(Channel channel, DiscordClient client) => client.Modules().Modules
            .Where(m => m.Id == nameof(ChannelWhitelistModule).ToLowerInvariant())
            .SingleOrDefault()?
            .EnabledChannels
            .Contains(channel) ?? false;

        internal static long[] ConvertStringArrayToLongArray(params string[] strings)
        {
            var longs = new List<long>();
            foreach (var s in strings)
            {
                long temp;
                if (Int64.TryParse(s, out temp))
                {
                    longs.Add(temp);
                }
            }

            return longs.ToArray();
        }

        internal static string FormatServantProfile(ServantProfile profile)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"**Servant:** {profile.Name}")
                .AppendLine($"**Class** {profile.Class}")
                .AppendLine($"**Rarity:** {profile.Rarity}")
                .AppendLine($"**Collection ID:** {profile.Id}")
                .AppendLine($"**Card pool:** {profile.Cards}")
                .AppendLine($"**Max ATK:** {profile.Attack}")
                .AppendLine($"**Max HP:** {profile.Health}")
                .AppendLine($"**Growth type:** {profile.GrowthCurve}")
                .AppendLine($"**NP:** {profile.NP} - *{profile.NPEffect}*")
                .AppendLine($"**Skill 1:** {profile.Skill1} - *{profile.Skill1Effect}*")
                .AppendLine($"**Skill 2:** {profile.Skill2} - *{profile.Skill2Effect}*");
            if (!String.IsNullOrWhiteSpace(profile.Skill3))
            {
                sb.AppendLine($"**Skill 3:** {profile.Skill3} - *{profile.Skill3Effect}*");
            }
            if (!String.IsNullOrWhiteSpace(profile.Passive1))
            {
                sb.AppendLine($"**Passive 1:** {profile.Passive1} - *{profile.Passive1Effect}*");
                if (!String.IsNullOrWhiteSpace(profile.Passive2))
                {
                    sb.AppendLine($"**Passive 2:** {profile.Passive2} - *{profile.Passive2Effect}*");
                    if (!String.IsNullOrWhiteSpace(profile.Passive3))
                    {
                        sb.AppendLine($"**Passive 3:** {profile.Passive3} - *{profile.Passive3Effect}*");
                        if (!String.IsNullOrWhiteSpace(profile.Passive4))
                        {
                            sb.AppendLine($"**Passive 4:** {profile.Passive4} - *{profile.Passive4Effect}*");
                        }
                    }
                }
            }
            sb.AppendLine($"{profile.ImageLink}");
            return sb.ToString();
        }
    }

    internal struct DateTimeWithZone
    {
        private readonly DateTime utcDateTime;
        private readonly TimeZoneInfo timeZone;

        public DateTimeWithZone(DateTime dateTimeUtc, TimeZoneInfo timeZone)
        {
            utcDateTime = dateTimeUtc;
            this.timeZone = timeZone;
        }

        public DateTime UniversalTime { get { return utcDateTime; } }

        public TimeZoneInfo TimeZone { get { return timeZone; } }

        public DateTime LocalTime
        {
            get
            {
                return TimeZoneInfo.ConvertTime(utcDateTime, timeZone);
            }
        }
    }
}
