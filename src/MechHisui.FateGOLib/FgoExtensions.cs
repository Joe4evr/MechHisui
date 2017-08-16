using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MechHisui.FateGOLib.Modules;
using SharedExtensions;

namespace MechHisui.FateGOLib
{
    public static class FgoExtensions
    {
        public static Task UseFgoService(
            this CommandService commands,
            IServiceCollection map,
            FgoConfig config,
            DiscordSocketClient client)
        {
            var statService = new FgoStatService(config, client);
            map.AddSingleton(statService);
            return Task.WhenAll(
                commands.AddModuleAsync<ServantModule>(),
                commands.AddModuleAsync<CEModule>(),
                commands.AddModuleAsync<MysticModule>(),
                commands.AddModuleAsync<EventsModule>()
            );
        }

        internal static EmbedBuilder AddFieldSequence<T>(
            this EmbedBuilder builder,
            IEnumerable<T> seq,
            Action<EmbedFieldBuilder, T> action)
        {
            foreach (var item in seq)
            {
                builder.AddField(efb => action(efb, item));
            }

            return builder;
        }

        internal static EmbedBuilder AddFieldWhen(
            this EmbedBuilder builder,
            Func<bool> predicate,
            Action<EmbedFieldBuilder> field)
        {
            if (predicate())
            {
                builder.AddField(field);
            }

            return builder;
        }

        internal static EmbedBuilder WithImageWhen(
            this EmbedBuilder builder,
            Func<bool> predicate,
            string img)
        {
            if (predicate())
            {
                builder.WithImageUrl(img);
            }

            return builder;
        }

        internal static EmbedBuilder WithDescriptionWhen(
            this EmbedBuilder builder,
            Func<bool> predicate,
            string description)
        {
            if (predicate())
            {
                builder.WithDescription(description);
            }

            return builder;
        }

        internal static string ToNiceString(this TimeSpan ts)
        {
            var d = ts.TotalDays == 1 ? "day" : "days";
            var h = ts.Hours == 1 ? "hour" : "hours";
            var m = ts.Minutes == 1 ? "minute" : "minutes";

            return (ts.TotalHours > 24)
                ? $"{ts.Days} {d} and {ts.Hours} {h}"
                : $"{ts.Hours} {h} and {ts.Minutes} {m}";
        }

        private static readonly Platform _platform = RuntimeEnvironment.OperatingSystemPlatform;
        internal static TimeZoneInfo JpnTimeZone => _platform == Platform.Windows
                    ? TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time")
                    : TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");

        public static IEnumerable<ServantProfile> WhereActive(this IEnumerable<ServantProfile> profiles, string skill)
            => profiles.Where(p => p.ActiveSkills.Any(s => s.SkillName == skill));

        public static IEnumerable<ServantProfile> WhereActiveEffect(this IEnumerable<ServantProfile> profiles, string effect)
            => profiles.Where(p => p.ActiveSkills.Any(s => s.Effect.Contains(effect)));

        public static IEnumerable<ServantProfile> WherePassive(this IEnumerable<ServantProfile> profiles, string skill)
            => profiles.Where(p => p.PassiveSkills.Any(s => s.SkillName == skill));

        public static IEnumerable<ServantProfile> WherePassiveEffect(this IEnumerable<ServantProfile> profiles, string effect)
            => profiles.Where(p => p.PassiveSkills.Any(s => s.Effect.Contains(effect)));

        public static IEnumerable<ServantProfile> WhereTrait(this IEnumerable<ServantProfile> profiles, string trait)
            => profiles.Where(p => p.Traits.Any(t => t.ContainsIgnoreCase(trait)));
    }
}
