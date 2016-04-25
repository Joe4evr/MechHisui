using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Configuration;
using JiiLib;
using Discord;
using Discord.Commands;
using Discord.Modules;
using System.IO;
using System.Reflection;

namespace MechHisui.Modules
{
    public class EvalModule : IModule
    {
        private const string regex = @"`{3}(?:\S*$)((?:.*\n)*)`{3}";
        private readonly IConfiguration _config;
        private readonly IEnumerable<MetadataReference> _references;
        private readonly string _syntaxText;

        private EvalModule(
            IConfiguration config,
            IEnumerable<MetadataReference> references,
            string syntax)
        {
            _config = config;
            _references = references;
            _syntaxText = syntax;
        }

        public void Install(ModuleManager manager)
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
                        Regex.Replace(arg, regex, @"{ \1 }");
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

        public class Builder
        {
            private List<MetadataReference> _references = new List<MetadataReference>();
            private List<string> _usings = new List<string>();

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

            public Builder Add(EvalReference reference)
            {
                _references.Add(reference.Reference);
                _usings.AddRange(reference.Namespaces);
                return this;
            }

            public EvalModule Build(IConfiguration config)
            {
                var sb = new StringBuilder()
                    .AppendSequence(_usings, (s, str) => s.AppendLine($"using {str};"))
                    .Append(@"namespace DynamicCompile
{{
    public class DynEval
    {{
        public async Task<string> Eval<T>(Func<Task<IEnumerable<T>>> set) => String.Join("", "", await set());

        public async Task<string> Eval<T>(Func<Task<T>> func) => (await func())?.ToString() ?? ""null"";

        public async Task<string> Eval(Action<Task> func) => (await func()?.ContinueWith(t => ""Executed"")) ?? ""null"";

        public async Task<string> Exec(DiscordClient client, CommandEventArgs e) => await Eval(async () => {0});
    }}
}}");
                return new EvalModule(config, _references, sb.ToString());
            }
        }
    }

    public class EvalReference
    {
        public IEnumerable<string> Namespaces { get; }
        public MetadataReference Reference { get; }

        public EvalReference(MetadataReference reference, params string[] namespaces)
        {
            Reference = reference;
            Namespaces = namespaces;
        }
    }
}
