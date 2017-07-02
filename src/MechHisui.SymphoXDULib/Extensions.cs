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
    }
}
