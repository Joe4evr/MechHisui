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
            .SingleOrDefault(m => m.Id == nameof(ChannelWhitelistModule).ToLowerInvariant())?
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
                .AppendLine($"**Class:** {profile.Class}")
                .AppendLine($"**Rarity:** {profile.Rarity}")
                .AppendLine($"**Collection ID:** {profile.Id}")
                .AppendLine($"**Card pool:** {profile.CardPool}")
                .AppendLine($"**Max ATK:** {profile.Atk}")
                .AppendLine($"**Max HP:** {profile.HP}")
                .AppendLine($"**Growth type:** {profile.GrowthCurve}")
                .AppendLine($"**NP:** {profile.NoblePhantasm} - *{profile.NoblePhantasmEffect}*");
            if (!String.IsNullOrWhiteSpace(profile.Skill1))
            { 
                sb.AppendLine($"**Skill 1:** {profile.Skill1} - *{profile.Effect1}*");
            }
            if (!String.IsNullOrWhiteSpace(profile.Skill2))
            {
                sb.AppendLine($"**Skill 2:** {profile.Skill2} - *{profile.Effect2}*");
            }
            if (!String.IsNullOrWhiteSpace(profile.Skill3))
            {
                sb.AppendLine($"**Skill 3:** {profile.Skill3} - *{profile.Effect3}*");
            }
            if (!String.IsNullOrWhiteSpace(profile.PassiveSkill1))
            {
                sb.AppendLine($"**Passive 1:** {profile.PassiveSkill1} - *{profile.PEffect1}*");
                if (!String.IsNullOrWhiteSpace(profile.PassiveSkill2))
                {
                    sb.AppendLine($"**Passive 2:** {profile.PassiveSkill2} - *{profile.PEffect2}*");
                    if (!String.IsNullOrWhiteSpace(profile.PassiveSkill3))
                    {
                        sb.AppendLine($"**Passive 3:** {profile.PassiveSkill3} - *{profile.PEffect3}*");
                        if (!String.IsNullOrWhiteSpace(profile.PassiveSkill4))
                        {
                            sb.AppendLine($"**Passive 4:** {profile.PassiveSkill4} - *{profile.PEffect4}*");
                        }
                    }
                }
            }
            sb.AppendLine($"{profile.Image}");
            return sb.ToString();
        }

        internal static IEnumerable<Channel> IterateChannels(IEnumerable<Server> servers, bool printServerNames = false, bool printChannelNames = false)
        {
            foreach (var server in servers)
            {
                if (printServerNames)
                {
                    Console.WriteLine(server.Name + "\n");
                }
                foreach (var channel in server.Channels)
                {
                    if (printChannelNames)
                    {
                        Console.WriteLine($"{channel.Name}:  {channel.Id}");
                    }
                    yield return channel;
                }
            }
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

        public DateTime UniversalTime => utcDateTime;

        public TimeZoneInfo TimeZone => timeZone;

        public DateTime LocalTime => TimeZoneInfo.ConvertTime(utcDateTime, timeZone);
    }
}
