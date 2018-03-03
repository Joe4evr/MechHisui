using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace SharedExtensions
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
        public static ICollection<T> Shuffle<T>(
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
            this IReadOnlyDictionary<TKey, TValue> source,
            TKey key,
            TValue defaultValue = default)
        {
            return source.TryGetValue(key, out var ret) ? ret : defaultValue;
        }

        public static IEnumerable<TResult> DictionarySelect<TKey, TValue, TResult>(
            this IDictionary<TKey, TValue> source,
            Func<TKey, TValue, TResult> selector)
                => source.Select(kvp => selector(kvp.Key, kvp.Value));

        public static void InsertAt<T>(this Stack<T> source, uint index, T item)
        {
            if (index > source.Count)
                throw new ArgumentOutOfRangeException("Insertion index may not be greater than the stack's current size.", nameof(index));

            if (index == 0)
            {
                source.Push(item);
                return;
            }

            var buffer = new T[index];
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = source.Pop();

            source.Push(item);
            for (int i = buffer.Length - 1; i >= 0; i--)
                source.Push(buffer[i]);
        }

        public static T TakeAt<T>(this IList<T> source, int index)
        {
            var result = source[index];
            if (result != null)
            {
                source.RemoveAt(index);
            }
            return result;
        }

        public static T TakeFirstOrDefault<T>(this IList<T> source, Func<T, bool> predicate)
        {
            var result = source.FirstOrDefault(predicate);
            if (result != null)
            {
                source.Remove(result);
            }
            return result;
        }

        public static IEnumerable<TResult> Permutate<T1, T2, TResult>(
            this IEnumerable<T1> source1,
            IEnumerable<T2> source2,
            Func<T1, T2, TResult> selector)
        {
            foreach (var item1 in source1)
            {
                foreach (var item2 in source2)
                {
                    yield return selector(item1, item2);
                }
            }
        }
    }
}
