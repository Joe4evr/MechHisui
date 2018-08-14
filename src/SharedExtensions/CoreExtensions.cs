using System;
using System.Collections.Generic;
using System.Linq;

namespace SharedExtensions
{
    internal static class CoreExtensions
    {
        /// <summary>
        ///     Indicates whether a collection of strings contains a given string case-invariantly.
        /// </summary>
        /// <param name="haystack">
        ///     The collection of strings.
        /// </param>
        /// <param name="needle">
        ///     The string to find.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if at least one item is case-invariantly equal to the provided string, otherwise <see langword="false"/> .
        /// </returns>
        public static bool ContainsIgnoreCase(
            this IEnumerable<string> haystack,
            string needle,
            StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            switch (comparison)
            {
                case StringComparison.CurrentCulture:
                case StringComparison.InvariantCulture:
                case StringComparison.Ordinal:
                    throw new ArgumentException(message: "Comparison must be of the *IgnoreCase variety.", paramName: nameof(comparison));
                default:
                    break;
            }

            return haystack.Any(s => s.Equals(needle, comparison));
        }

        /// <summary>
        ///     Indicates whether a string contains a given substring case-invariantly.
        /// </summary>
        /// <param name="haystack">
        ///     The string to search in.
        /// </param>
        /// <param name="needle">
        ///     The substring to find.
        /// </param>
        /// <returns>
        ///     <see langword="true"/>  if the string case-invariantly contains the provided substring, otherwise <see langword="false"/> .
        /// </returns>
        public static bool ContainsIgnoreCase(
            this string haystack,
            string needle,
            StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            switch (comparison)
            {
                case StringComparison.CurrentCulture:
                case StringComparison.InvariantCulture:
                case StringComparison.Ordinal:
                    throw new ArgumentException(message: "Comparison must be of the *IgnoreCase variety.", paramName: nameof(comparison));
                default:
                    break;
            }

            return haystack.IndexOf(needle, comparison) >= 0;
        }

        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue val)
        {
            key = kvp.Key;
            val = kvp.Value;
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
