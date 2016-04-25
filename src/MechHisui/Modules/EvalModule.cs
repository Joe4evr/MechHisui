﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Configuration;
using JiiLib;
using Discord.Commands;
using Discord.Modules;

namespace MechHisui.Modules
{
    /// <summary>
    /// Module for runtime evaluation of code.
    /// Use <see cref="EvalModule.Builder"/> to create an instance of this class.
    /// </summary>
    public class EvalModule : IModule
    {
        /// <summary>
        /// Regex string to detect markdown code blocks.
        /// </summary>
        private const string regex = @"`{3}(?:\S*$)((?:.*\n)*)`{3}";

        /// <summary>
        /// An <see cref="IConfiguration"/> object that holds additional data.
        /// </summary>
        private readonly IConfiguration _config;

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
        /// Creates a new <see cref="EvalModule"/>.
        /// </summary>
        /// <param name="references">The list of assemblies to reference.</param>
        /// <param name="syntax">The text to parse into a <see cref="SyntaxTree"/>.</param>
        /// <param name="config">An optional <see cref="IConfiguration"/>
        /// object that holds additional data.</param>
        private EvalModule(
            IEnumerable<MetadataReference> references,
            string syntax,
            IConfiguration config = null)
        {
            _config = config;
            _references = references;
            _syntaxText = syntax;
        }
        
        void IModule.Install(ModuleManager manager)
        {
            Console.WriteLine("Registering 'Eval'...");
            manager.Client.GetService<CommandService>().CreateCommand("eval")
                .Parameter("func", ParameterType.Unparsed)
                .Hide()
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(_config["Owner"]) || ch.Id == UInt64.Parse(_config["FGO_general"]) || ch.Id == UInt64.Parse(_config["FGO_playground"]))
                .Do(async cea =>
                {
                    string arg = cea.Args[0];
                    if (arg.Contains('^'))
                    {
                        await cea.Channel.SendMessage("**Note:** `^` is the Binary XOR operator. Use `Math.Pow(base, exponent)` if you wish to calculate an exponentiation.");
                    }
                    if (Regex.Match(arg, regex, RegexOptions.Multiline).Success)
                    {
                        arg = Regex.Replace(arg, regex, @"{$1}", RegexOptions.Multiline);
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
                            Assembly assembly = Assembly.Load(ms.ToArray());

                            Type type = assembly.GetType("DynamicCompile.DynEval");
                            object obj = Activator.CreateInstance(type);
                            var res = (await (Task<string>)type.InvokeMember("Exec",
                                BindingFlags.Default | BindingFlags.InvokeMethod,
                                null,
                                obj,
                                new object[2] { manager.Client, cea }));

                            await cea.Channel.SendMessage($"**Result:** {res}");
                        }
                        else
                        {
                            IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                                diagnostic.IsWarningAsError ||
                                diagnostic.Severity == DiagnosticSeverity.Error);

                            Console.Error.WriteLine(String.Join("\n", failures.Select(f => $"{f.Id}: {f.GetMessage()}")));
                            await cea.Channel.SendMessage($"**Error:** {failures.First().GetMessage()}");
                        }
                    }
                });
        }

        /// <summary>
        /// Builder for an <see cref="EvalModule"/>.
        /// </summary>
        public class Builder
        {
            private List<MetadataReference> _references = new List<MetadataReference>();
            private List<string> _usings = new List<string>();

            /// <summary>
            /// Creates a <see cref="Builder"/> for an <see cref="EvalModule"/> with
            /// predefined references to <see cref="System"/>, 
            /// <see cref="System.Collections.Generic"/>,
            /// <see cref="System.Threading.Tasks"/>, and <see cref="System.Linq"/>.
            /// </summary>
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
            /// Builds the <see cref="EvalModule"/> from the current <see cref="Builder"/> instance.
            /// </summary>
            /// <param name="config">An optional <see cref="IConfiguration"/>
            /// containing information that may be used inside of the 
            /// <see cref="EvalModule"/>.</param>
            public EvalModule Build(IConfiguration config = null)
            {
                var sb = new StringBuilder()
                    .AppendSequence(_usings, (s, str) => s.AppendLine($"using {str};"))
                    .Append(@"namespace DynamicCompile
{{
    public class DynEval
    {{
        public async Task<string> Eval<T>(Func<Task<IEnumerable<T>>> set) => String.Join("", "", await set());

        public async Task<string> Eval<T>(Func<Task<T>> func) => (await func())?.ToString() ?? ""null"";

        public async Task<string> Eval(Func<Task> func) => (await func()?.ContinueWith(t => ""Executed"")) ?? ""null"";

        public async Task<string> Exec(DiscordClient client, CommandEventArgs e) => await Eval(async () => await Task.Run(() => {0}));
    }}
}}");
                return new EvalModule(_references, sb.ToString(), config);
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

    //public class DynEval
    //{
    //    public async Task<string> Eval<T>(Func<Task<IEnumerable<T>>> set) => String.Join(", ", await set());

    //    public async Task<string> Eval<T>(Func<Task<T>> func) => (await func())?.ToString() ?? "null";

    //    public async Task<string> Eval(Func<Task> func) => (await func()?.ContinueWith(t => "Executed")) ?? "null";

    //    public async Task<string> Exec(DiscordClient client, CommandEventArgs e) => await Eval(
    //        async () => await Task.Run(
    //            () => {
    //                var str1 = "This is a multi-line eval";
    //                var str2 = "Now it's easier to eval some more complex things";
    //                return String.Concat(str1, "\n", str2);
    //            }));
    //}
}
