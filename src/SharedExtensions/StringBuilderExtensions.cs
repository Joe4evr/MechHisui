using System.Collections.Generic;
using System.Linq;

namespace System.Text
{
    internal static class StringBuilderExtensions
    {
        /// <summary> Appends a string to a <see cref="StringBuilder"/>
        /// instance only if a condition is met. </summary>
        /// <param name="builder">A <see cref="StringBuilder"/> instanc</param>
        /// <param name="predicate">The condition to be met.</param>
        /// <param name="fn">The function to apply if predicate is true.</param>
        /// <returns>A <see cref="StringBuilder"/> instance with the specified
        /// string appended if predicate was true,
        /// or the unchanged <see cref="StringBuilder"/> instance otherwise.</returns>
        /// <exception cref="ArgumentNullException">Any argument is null.</exception>
        public static StringBuilder AppendWhen(
            this StringBuilder builder,
            Func<bool> predicate,
            Func<StringBuilder, StringBuilder> fn)
        {
            builder = builder ?? throw new ArgumentNullException(nameof(builder));
            predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
            fn = fn ?? throw new ArgumentNullException(nameof(fn));

            return predicate() ? fn(builder) : builder;
        }

        /// <summary> Appends each element of an <see cref="IEnumerable{T}"/>
        /// to a <see cref="StringBuilder"/> instance. </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder">A <see cref="StringBuilder"/> instance</param>
        /// <param name="seq">The sequence to append.</param>
        /// <param name="fn">The function to apply to each element of the sequence.</param>
        /// <returns>An instance of <see cref="StringBuilder"/> with all elements of seq appended.</returns>
        /// <exception cref="ArgumentNullException">Any argument is null.</exception>
        public static StringBuilder AppendSequence<T>(
            this StringBuilder builder,
            IEnumerable<T> seq,
            Func<StringBuilder, T, StringBuilder> fn)
        {
            builder = builder ?? throw new ArgumentNullException(nameof(builder));
            seq = seq ?? throw new ArgumentNullException(nameof(seq));
            fn = fn ?? throw new ArgumentNullException(nameof(fn));

            return seq.Aggregate(builder, fn);
        }
    }
}
