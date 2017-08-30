using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Discord;
using SharedExtensions;

namespace Kohaku
{
    public sealed partial class EvalService
    {
        /// <summary> Builder for an <see cref="EvalService"/>. </summary>
        public class Builder
        {
            private readonly HashSet<string> _usings = new HashSet<string>();
            private readonly HashSet<MetadataReference> _references = new HashSet<MetadataReference>(new MdrEq());

            /// <summary> Creates a <see cref="Builder"/> for an <see cref="EvalService"/> with
            /// predefined references to <see cref="System"/>, 
            /// <see cref="System.Collections.Generic"/>,
            /// <see cref="System.Threading.Tasks"/>, and <see cref="System.Linq"/>. </summary>
            public static Builder BuilderWithSystemAndLinq()
            {
                return new Builder()
                    .Add(new EvalReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                        "System",
                        "System.Collections.Generic",
                        "System.Threading.Tasks"))
                    .Add(new EvalReference(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                        "System.Linq"));
            }

            /// <summary> Adds an <see cref="EvalReference"/> to the
            /// <see cref="Builder"/> instance. </summary>
            /// <param name="reference">An instance of <see cref="EvalReference"/>
            /// to add to the Module.</param>
            public Builder Add(EvalReference reference)
            {
                _references.Add(reference.Reference);
                foreach (var ns in reference.Namespaces)
                {
                    _usings.Add(ns);
                }
                return this;
            }

            /// <summary> Builds the <see cref="EvalModule"/> from the
            /// current <see cref="Builder"/> instance. </summary>
            /// <param name="checkFunc">A function for checking when
            /// the command is allowed to run.</param>
            public EvalService Build(Func<LogMessage, Task> logger = null, params(Type Type, string FieldName)[] argTypes)
            {
                var fields = System.String.Join("\n", argTypes.Select(((Type Type, string FieldName) t) => $"private readonly {t.Type.Name} {t.FieldName};"));
                var ctorArgs = $"public DynEval({System.String.Join(", ", argTypes.Select(((Type Type, string FieldName) t) => $"{t.Type.Name} {t.FieldName}"))})";
                var assigns = System.String.Join(";\n", argTypes.Select(((Type Type, string FieldName) t) => $"this.{t.FieldName} = {t.FieldName};"));
                var ctor = $@"{fields}
{ctorArgs}
{{
{assigns}
}}";

                var sb = new StringBuilder()
                    .AppendSequence(_usings, (s, str) => s.AppendLine($"using {str};"))
                    .Append(Regex.Replace(@"namespace DynamicCompile
{
    public class DynEval
    {
        {ctor}

        private async Task<string> Eval<T>(Func<Task<IEnumerable<T>>> set) => String.Join("", "", await set.Invoke());
        private async Task<string> Eval<T>(Func<Task<T>> func) => (await func.Invoke()).ToString() ?? ""null"";
        private async Task<string> Eval(Func<Task> func) { await func.Invoke(); return ""Executed""; }
        public async Task<string> Exec() => await Eval(async () => {expr});
    }
}", @"{ctor}", ctor));
                string v = sb.ToString();
                return new EvalService(_references, v, logger);
            }

            private class MdrEq : EqualityComparer<MetadataReference>
            {
                public override bool Equals(MetadataReference x, MetadataReference y)
                {
                    return x?.Display == y?.Display;
                }

                public override int GetHashCode(MetadataReference obj)
                {
                    return obj?.Display.GetHashCode() ?? 0;
                }
            }
        }
    }
}
