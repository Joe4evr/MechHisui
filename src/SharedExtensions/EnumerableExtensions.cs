using System;
using System.Linq;
using System.Security.Cryptography;

namespace System.Collections.Generic
{
    internal static class EnumerableExtensions
    {
        //Method for randomizing lists using a Fisher-Yates shuffle.
        //Based on http://stackoverflow.com/questions/273313/
        /// <summary> Perform a Fisher-Yates shuffle on a collection implementing <see cref="IEnumerable{T}"/>. </summary>
        /// <param name="source">The items to shuffle.</param>
        /// <param name="iterations">The amount of times the itmes should be shuffled.</param>
        /// <typeparam name="T"></typeparam>
        /// <remarks>Adapted from http://stackoverflow.com/questions/273313/. </remarks>
        public static IEnumerable<T> Shuffle<T>(
            this IEnumerable<T> source,
            uint iterations = 1)
        {
            iterations = (iterations == 0) ? 1 : iterations;

            var provider = RandomNumberGenerator.Create();
            var buffer = source.ToList();
            int n = buffer.Count;
            for (uint i = 0; i < iterations; i++)
            {
                while (n > 1)
                {
                    var box = new byte[(n / Byte.MaxValue) + 1];
                    int boxSum;
                    do
                    {
                        provider.GetBytes(box);
                        boxSum = box.Sum(b => b);
                    }
                    while (!(boxSum < n * ((Byte.MaxValue * box.Length) / n)));
                    int k = boxSum % n;
                    n--;
                    var value = buffer[k];
                    buffer[k] = buffer[n];
                    buffer[n] = value;
                }
            }

            return buffer;
        }

        public static TValue GetValueOrDefault<TKey, TValue>(
            this IReadOnlyDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue defaultValue = default(TValue))
        {
            return dictionary.TryGetValue(key, out var ret) ? ret : defaultValue;
        }
    }
}
