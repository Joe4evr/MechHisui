using System;
using System.Collections.Generic;

namespace Discord
{
    internal static class DiscordExtensions
    {
        internal static bool HasPerms(this IGuildUser user, IGuildChannel channel, DiscordPermissions perms)
        {
            var clientPerms = (DiscordPermissions)user.GetPermissions(channel).RawValue;
            return (clientPerms & perms) == perms;
        }

        [Flags]
        internal enum DiscordPermissions : ulong
        {
            None = 0,
            CREATE_INSTANT_INVITE = 0x00000001,
            KICK_MEMBERS = 0x00000002,
            BAN_MEMBERS = 0x00000004,
            ADMINISTRATOR = 0x00000008,
            MANAGE_CHANNELS = 0x00000010,
            MANAGE_GUILD = 0x00000020,
            ADD_REACTIONS = 0x00000040,
            READ_MESSAGES = 0x00000400,
            SEND_MESSAGES = 0x00000800,
            SEND_TTS_MESSAGES = 0x00001000,
            MANAGE_MESSAGES = 0x00002000,
            EMBED_LINKS = 0x00004000,
            ATTACH_FILES = 0x00008000,
            READ_MESSAGE_HISTORY = 0x00010000,
            MENTION_EVERYONE = 0x00020000,
            USE_EXTERNAL_EMOJIS = 0x00040000,
            CONNECT = 0x00100000,
            SPEAK = 0x00200000,
            MUTE_MEMBERS = 0x00400000,
            DEAFEN_MEMBERS = 0x00800000,
            MOVE_MEMBERS = 0x01000000,
            USE_VAD = 0x02000000,
            CHANGE_NICKNAME = 0x04000000,
            MANAGE_NICKNAMES = 0x08000000,
            MANAGE_ROLES = 0x10000000,
            MANAGE_WEBHOOKS = 0x20000000,
            MANAGE_EMOJIS = 0x40000000,
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

        internal static EmbedBuilder WithImageIfNotNull(
            this EmbedBuilder builder,
            string img)
        {
            if (!String.IsNullOrWhiteSpace(img))
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
    }
}
