using System;
using System.Collections.Generic;
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

        internal static string ToNiceString(this TimeSpan ts)
        {
            var d = ts.TotalDays == 1 ? "day" : "days";
            var h = ts.Hours == 1 ? "hour" : "hours";
            var m = ts.Minutes == 1 ? "minute" : "minutes";

            return (ts.TotalHours > 24)
                ? $"{ts.Days} {d} and {ts.Hours} {h}"
                : $"{ts.Hours} {h} and {ts.Minutes} {m}";
        }
    }
}
