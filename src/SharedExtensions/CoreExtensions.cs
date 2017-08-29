using System;
using System.Collections.Generic;
using System.Linq;

namespace SharedExtensions
{
    internal static class CoreExtensions
    {
        /// <summary>
        /// Indicates whether a <see cref="IEnumerable{String}"/> contains a given string case-invariantly.
        /// </summary>
        /// <param name="haystack">The collection of strings.</param>
        /// <param name="needle">The string to find.</param>
        /// <returns>True if at least one item is case-invariantly equal to the provided string, otherwise false.</returns>
        public static bool ContainsIgnoreCase(this IEnumerable<string> haystack, string needle)
            => haystack.Any(s => s.ToLowerInvariant() == needle.ToLowerInvariant());

        /// <summary>
        /// Indicates whether a <see cref="String"/> contains a given substring case-invariantly.
        /// </summary>
        /// <param name="haystack">The string to search in.</param>
        /// <param name="needle">The substring to find.</param>
        /// <returns>True if the string case-invariantly contains the provided substring, otherwise false.</returns>
        public static bool ContainsIgnoreCase(this string haystack, string needle)
            => haystack.ToLowerInvariant().Contains(needle.ToLowerInvariant());
    }
}
