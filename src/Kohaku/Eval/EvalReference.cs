using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using System;

namespace Kohaku
{
    /// <summary> Container for a <see cref="MetadataReference"/> and associated namespaces. </summary>
    public class EvalReference
    {
        /// <summary> The set of namespaces to be imported for evaluation. </summary>
        public IEnumerable<string> Namespaces { get; }

        /// <summary> The <see cref="MetadataReference"/> contained in this instance. </summary>
        public MetadataReference Reference { get; }

        /// <summary> Creates a new instance of <see cref="EvalReference"/>. </summary>
        /// <param name="reference">A <see cref="MetadataReference"/>
        /// pointing to the assembly you wish to reference.</param>
        /// <param name="namespaces">One or more namespaces defined in the
        /// referenced assembly to import.</param>
        public EvalReference(MetadataReference reference, params string[] namespaces)
        {
            Reference = reference;
            Namespaces = namespaces;
        }

        public EvalReference(Type type)
            : this(MetadataReference.CreateFromFile(type.Assembly.Location), type.Namespace)
        {
        }
    }
}
