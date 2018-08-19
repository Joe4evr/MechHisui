using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MechHisui
{
    public sealed class CompositeException : Exception
    {
        public IReadOnlyList<Exception> InnerExceptions { get; }

        public CompositeException(params Exception[] exceptions)
            : base("One or more exceptions have occured.")
        {
            InnerExceptions = exceptions.ToImmutableArray();
        }
    }
}
