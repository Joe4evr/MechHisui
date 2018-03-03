using System;
using System.Collections.Generic;
using System.Linq;
using Discord;

namespace MechHisui.SymphoXDULib
{
    internal static class Extensions
    {
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
            bool predicate,
            Action<EmbedFieldBuilder> field)
        {
            if (predicate)
            {
                builder.AddField(field);
            }

            return builder;
        }

        internal static EmbedBuilder AddImageChecked(
            this EmbedBuilder builder,
            string imageUrl)
        {
            if (!String.IsNullOrWhiteSpace(imageUrl))
            {
                builder.WithImageUrl(imageUrl);
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

        internal static IEnumerable<Embed> ToEmbedPages(this IEnumerable<IXduProfile> profiles)
        {
            return profiles.Select(XduModule.XduCharacters.FormatCharacter);
            //return profiles.Select(profile => new EmbedBuilder()
            //    .WithAuthor(auth => auth.WithName($"Symphogear #{profile.Id}: {Rarity}☆ Element: {element}"))
            //    .WithTitle($"{CharacterName}: {profile.Skills[1].SkillName}")
            //    .WithDescription("Detailed stats not known yet.")
            //    .AddFieldWhen(profile.HP > 0,
            //        field => field.WithIsInline(true)
            //            .WithName("Max HP")
            //            .WithValue(profile.HP.ToString()))
            //    .AddFieldWhen(profile.Atk > 0,
            //        field => field.WithIsInline(true)
            //            .WithName("Max ATK")
            //            .WithValue(profile.Atk.ToString()))
            //    .AddFieldWhen(profile.Def > 0,
            //        field => field.WithIsInline(true)
            //            .WithName("Max DEF")
            //            .WithValue(profile.Def.ToString()))
            //    .AddField(field => field.WithIsInline(true)
            //        .WithName("SPD")
            //        .WithValue(profile.Spd.ToString()))
            //    .AddField(field => field.WithIsInline(true)
            //        .WithName("Critical Rate")
            //        .WithValue($"{profile.Ctr}%"))
            //    .AddField(field => field.WithIsInline(true)
            //        .WithName("Critical Damage")
            //        .WithValue($"{profile.Ctd}%"))
            //    .AddFieldWhen(!String.IsNullOrWhiteSpace(profile.LeaderSkill),
            //        field => field.WithIsInline(false)
            //            .WithName("Leader Skill")
            //            .WithValue(profile.LeaderSkill))
            //    .AddFieldWhen(!String.IsNullOrWhiteSpace(profile.PassiveSkill),
            //        field => field.WithIsInline(false)
            //            .WithName("Passive Skill")
            //            .WithValue(profile.PassiveSkill))
            //    .AddFieldSequence(profile.Skills,
            //        (field, skill) => field.WithIsInline(false)
            //            .WithName($"{skill.SkillName} ({skill.SkillType})")
            //            .WithValue($"{skill.Range} {skill.Effect} CD:{skill.Cooldown}s"))
            //    .WithImageUrl(Image)
            //    .Build()).ToList();
        }
    }
}
