﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace MechHisui.Modules
{
    public class EvalService
    {
        /// <summary>
        /// Regex to detect markdown code blocks.
        /// </summary>
        private static readonly Regex _codeblock = new Regex(@"`{3}(?:\S*$)((?:.*\n)*)`{3}", RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>
        /// The assemblies referenced during evaluation.
        /// </summary>
        private readonly IEnumerable<MetadataReference> _references;

        /// <summary>
        /// The text that gets parsed into a
        /// <see cref="SyntaxTree"/> upon execution.
        /// </summary>
        private readonly string _syntaxText;

        /// <summary>
        /// Creates a new <see cref="EvalService"/>.
        /// </summary>
        /// <param name="references">The list of assemblies to reference.</param>
        /// <param name="syntax">The text to parse into a <see cref="SyntaxTree"/>.</param>
        /// <param name="config">An optional <see cref="IConfiguration"/>
        /// object that holds additional data.</param>
        private EvalService(
            IEnumerable<MetadataReference> references,
            string syntax)
        {
            _references = references;
            _syntaxText = syntax;
        }

        public async Task<string> Eval(string arg)
        {
            if (_codeblock.Match(arg).Success)
            {
                arg = _codeblock.Replace(arg, @"{$1}");
            }

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(String.Format(_syntaxText, arg));

            string assemblyName = Path.GetRandomFileName();
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName: assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: _references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (result.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    var assembly = AssemblyLoadContext.Default.LoadFromStream(ms);

                    var type = assembly.GetType("DynamicCompile.DynEval");
                    object obj = Activator.CreateInstance(type);
                    var res = (await (Task<string>)type.GetTypeInfo()
                        .GetDeclaredMethod("Exec")
                        .Invoke(obj, new object[0]));

                    return $"**Result:** {res}";
                }
                else
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    Console.Error.WriteLine(String.Join("\n", failures.Select(f => $"{f.Id}: {f.GetMessage()}")));
                    return $"**Error:** {failures.First().GetMessage()}";
                }
            }
        }

        /// <summary>
        /// Builder for an <see cref="EvalService"/>.
        /// </summary>
        public class Builder
        {
            private List<MetadataReference> _references = new List<MetadataReference>();
            private List<string> _usings = new List<string>();

            /// <summary>
            /// Creates a <see cref="Builder"/> for an <see cref="EvalService"/> with
            /// predefined references to <see cref="System"/>, 
            /// <see cref="System.Collections.Generic"/>,
            /// <see cref="System.Threading.Tasks"/>, and <see cref="System.Linq"/>.
            /// </summary>
            public static Builder BuilderWithSystemAndLinq()
            {
                return new Builder()
                    .Add(new EvalReference(MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                        "System",
                        "System.Collections.Generic",
                        "System.Threading.Tasks"))
                    .Add(new EvalReference(MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location),
                        "System.Linq"));
            }

            /// <summary>
            /// Adds an <see cref="EvalReference"/> to the
            /// <see cref="Builder"/> instance.
            /// </summary>
            /// <param name="reference">An instance of <see cref="EvalReference"/>
            /// to add to the Module.</param>
            public Builder Add(EvalReference reference)
            {
                if (!_references.Any(r => r.Display == reference.Reference.Display))
                {
                    _references.Add(reference.Reference);
                }
                foreach (var ns in reference.Namespaces)
                {
                    if (!_usings.Contains(ns))
                    {
                        _usings.Add(ns);
                    }
                }
                return this;
            }

            /// <summary>
            /// Builds the <see cref="EvalModule"/> from the
            /// current <see cref="Builder"/> instance.
            /// </summary>
            /// <param name="checkFunc">A function for checking when
            /// the command is allowed to run.</param>
            public EvalService Build()
            {
                var sb = new StringBuilder()
                    .AppendSequence(_usings, (s, str) => s.AppendLine($"using {str};"))
                    .Append(@"namespace DynamicCompile
        {{
            public class DynEval
            {{
                public string Eval<T>(Func<IEnumerable<T>> set) => String.Join("", "", set());

                public string Eval<T>(Func<T> func) => func()?.ToString() ?? ""null"";

                public string Eval(Action func) { func(); return ""Executed""; }

                public string Exec() => Eval(() => {0});
            }}
        }}");
                return new EvalService(_references, sb.ToString());
            }
        }
    }

    /// <summary>
    /// Container for a <see cref="MetadataReference"/> and associated namespaces.
    /// </summary>
    public class EvalReference
    {
        /// <summary>
        /// The set of namespaces to be imported for evaluation.
        /// </summary>
        public IEnumerable<string> Namespaces { get; }

        /// <summary>
        /// The <see cref="MetadataReference"/> contained in this instance.
        /// </summary>
        public MetadataReference Reference { get; }

        /// <summary>
        /// Creates a new instance of <see cref="EvalReference"/>.
        /// </summary>
        /// <param name="reference">A <see cref="MetadataReference"/>
        /// pointing to the assembly you wish to reference.</param>
        /// <param name="namespaces">One or more namespaces defined in the
        /// referenced assembly to import.</param>
        public EvalReference(MetadataReference reference, params string[] namespaces)
        {
            Reference = reference;
            Namespaces = namespaces;
        }
    }

    internal static class StringBuilderExt
    {
        /// <summary>
        /// Appends each element of an <see cref="IEnumerable{T}"/> to a <see cref="StringBuilder"/> instance.
        /// </summary>
        /// <param name="builder">A <see cref="StringBuilder"/> instance</param>
        /// <param name="seq">The sequence to append.</param>
        /// <param name="fn">A function to apply to each element of the sequence.</param>
        /// <returns>An instance of <see cref="StringBuilder"/> with all elements of <see cref="seq"/>appended.</returns>
        public static StringBuilder AppendSequence<T>(this StringBuilder builder, IEnumerable<T> seq, Func<StringBuilder, T, StringBuilder> fn)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (seq == null) throw new ArgumentNullException(nameof(seq));
            if (fn == null) throw new ArgumentNullException(nameof(fn));

            return seq.Aggregate(builder, fn);
        }
    }

    //public class DynEval
    //{
    //    public string Eval<T>(Func<IEnumerable<T>> set) => String.Join(", ", set());

    //    public string Eval<T>(Func<T> func) => func()?.ToString() ?? "null";

    //    public string Eval(Action func) { func(); return "Executed"; }

    //    public string Exec(DiscordClient client, CommandEventArgs e)
    //        => Eval(() => Console.WriteLine());
    //}
}
