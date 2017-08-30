using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord;

namespace Kohaku
{
    /// <summary> Service for runtime evaluation of code.
    /// Use <see cref="Builder"/> to create an instance of this class. </summary>
    public sealed partial class EvalService
    {
        /// <summary> Regex to detect markdown code blocks. </summary>
        private static readonly Regex _codeblock = new Regex(@"`{3}(?:\S*$)((?:.*\n)*)`{3}", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex _exprHole = new Regex(@"{expr}", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly object[] _emptyArray = Array.Empty<object>();

        /// <summary> The assemblies referenced during evaluation. </summary>
        private readonly IEnumerable<MetadataReference> _references;

        /// <summary> The text that gets parsed into a
        /// <see cref="SyntaxTree"/> upon execution. </summary>
        private readonly string _syntaxText;

        private readonly Func<LogMessage, Task> _logger;

        /// <summary> Creates a new <see cref="EvalService"/>. </summary>
        /// <param name="references">The list of assemblies to reference.</param>
        /// <param name="syntax">The text to parse into a <see cref="SyntaxTree"/>.</param>
        /// <param name="config">An optional <see cref="IConfiguration"/>
        /// object that holds additional data.</param>
        private EvalService(
            IEnumerable<MetadataReference> references,
            string syntax,
            Func<LogMessage, Task> logger = null)
        {
            _references = references;
            _syntaxText = syntax;
            _logger = logger ?? (_ => Task.CompletedTask);
        }

        public async Task<string> Eval(string arg, ICommandContext ctx, IServiceProvider services)
        {
            if (_codeblock.Match(arg).Success)
            {
                arg = _codeblock.Replace(arg, @"{$1}");
            }

            string tmp = _exprHole.Replace(_syntaxText, arg);
            var syntaxTree = CSharpSyntaxTree.ParseText(tmp);

            //TODO: lock out certain calls

            string assemblyName = Path.GetRandomFileName();
            var compilation = CSharpCompilation.Create(
                assemblyName: assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: _references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);

                if (result.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    var asmctx = AssemblyLoadContext.Default;
                    var assembly = asmctx.LoadFromStream(ms);

                    var type = assembly.GetType("DynamicCompile.DynEval");
                    object obj = ActivatorUtilities.CreateInstance(services, type, ctx);
                    var method = type.GetMethod("Exec", BindingFlags.Instance | BindingFlags.Public);
                    string res = await ((Task<string>)method.Invoke(obj, _emptyArray));

                    return $"**Result:** {res}";
                }
                else
                {
                    var failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                    Console.Error.WriteLine(String.Join("\n", failures.Select(f => $"{f.Id}: {f.GetMessage()}")));
                    return $"**Error:** {failures.First().GetMessage()}";
                }
            }
        }

        //private static void CheckSyntaxTree(CSharpSyntaxTree tree)
        //{
        //    var root = tree.GetCompilationUnitRoot();
        //    //root.
        //}
    }
}
