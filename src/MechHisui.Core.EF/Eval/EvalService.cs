//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using System.Runtime.Loader;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;
//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
//using SharedExtensions;

//namespace MechHisui.Core
//{
//    /// <summary> Service for runtime evaluation of code.
//    /// Use <see cref="Builder"/> to create an instance of this class. </summary>
//    public class EvalService
//    {
//        /// <summary> Regex to detect markdown code blocks. </summary>
//        private static readonly Regex _codeblock = new Regex(@"`{3}(?:\S*$)((?:.*\n)*)`{3}", RegexOptions.Compiled | RegexOptions.Multiline);
//        private static readonly Regex _exprHole = new Regex(@"{expr}", RegexOptions.Compiled | RegexOptions.Multiline);
//        //private static readonly object _emptyObj = new object();
//        private static readonly object[] _emptyArray = Array.Empty<object>();

//        /// <summary> The assemblies referenced during evaluation. </summary>
//        private readonly IEnumerable<MetadataReference> _references;

//        /// <summary> The text that gets parsed into a
//        /// <see cref="SyntaxTree"/> upon execution. </summary>
//        private readonly string _syntaxText;

//        /// <summary> Creates a new <see cref="EvalService"/>. </summary>
//        /// <param name="references">The list of assemblies to reference.</param>
//        /// <param name="services"></param>
//        /// <param name="syntax">The text to parse into a <see cref="SyntaxTree"/>.</param>
//        /// <param name="config">An optional <see cref="IConfiguration"/>
//        /// object that holds additional data.</param>
//        private EvalService(
//            IEnumerable<MetadataReference> references,
//            string syntax)
//        {
//            _references = references;
//            _syntaxText = syntax;
//        }

//        public async Task<string> Eval(string arg, IServiceProvider services = null)
//        {
//            if (_codeblock.Match(arg).Success)
//            {
//                arg = _codeblock.Replace(arg, @"{$1}");
//            }

//            string tmp = _exprHole.Replace(_syntaxText, arg);
//            var syntaxTree = CSharpSyntaxTree.ParseText(tmp);

//            string assemblyName = Path.GetRandomFileName();
//            var compilation = CSharpCompilation.Create(
//                assemblyName: assemblyName,
//                syntaxTrees: new[] { syntaxTree },
//                references: _references,
//                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

//            using (var ms = new MemoryStream())
//            {
//                var result = compilation.Emit(ms);

//                if (result.Success)
//                {
//                    ms.Seek(0, SeekOrigin.Begin);
//                    var assembly = AssemblyLoadContext.Default.LoadFromStream(ms);

//                    var type = assembly.GetType("DynamicCompile.DynEval");
//                    var obj = TypeDescriptor.CreateInstance(services, type, null, null);
//                    string res = await (Task<string>)type.GetTypeInfo()
//                        .GetMethod("Exec", BindingFlags.Static | BindingFlags.Public)
//                        .Invoke(obj, _emptyArray);

//                    return $"**Result:** {res}";
//                }
//                else
//                {
//                    var failures = result.Diagnostics.Where(diagnostic =>
//                        diagnostic.IsWarningAsError
//                        || diagnostic.Severity == DiagnosticSeverity.Error);

//                    Console.Error.WriteLine(String.Join("\n", failures.Select(f => $"{f.Id}: {f.GetMessage()}")));
//                    return $"**Error:** {failures.First().GetMessage()}";
//                }
//            }
//        }

//        /// <summary> Builder for an <see cref="EvalService"/>. </summary>
//        public class Builder
//        {
//            private readonly List<MetadataReference> _references = new List<MetadataReference>();
//            private readonly List<string> _usings = new List<string>();

//            /// <summary> Creates a <see cref="Builder"/> for an <see cref="EvalService"/> with
//            /// predefined references to <see cref="System"/>, 
//            /// <see cref="System.Collections.Generic"/>,
//            /// <see cref="System.Threading.Tasks"/>, and <see cref="System.Linq"/>. </summary>
//            public static Builder BuilderWithSystemAndLinq()
//            {
//                return new Builder()
//                    .Add(new EvalReference(MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
//                        "System",
//                        "System.Collections.Generic",
//                        "System.Threading.Tasks"))
//                    .Add(new EvalReference(MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location),
//                        "System.Linq"));
//            }

//            /// <summary> Adds an <see cref="EvalReference"/> to the
//            /// <see cref="Builder"/> instance. </summary>
//            /// <param name="reference">An instance of <see cref="EvalReference"/>
//            /// to add to the Module.</param>
//            public Builder Add(EvalReference reference)
//            {
//                if (!_references.Any(r => r.Display == reference.Reference.Display))
//                {
//                    _references.Add(reference.Reference);
//                }
//                foreach (var ns in reference.Namespaces)
//                {
//                    if (!_usings.Contains(ns))
//                    {
//                        _usings.Add(ns);
//                    }
//                }
//                return this;
//            }

//            /// <summary> Builds the <see cref="EvalModule"/> from the
//            /// current <see cref="Builder"/> instance. </summary>
//            /// <param name="checkFunc">A function for checking when
//            /// the command is allowed to run.</param>
//            /// <param name="services"></param>
//            /// <param name="ctor"></param>
//            public EvalService Build(string ctor = "(){}")
//            {
//                var sb = new StringBuilder()
//                    .AppendSequence(_usings, (s, str) => s.AppendLine($"using {str};"))
//                    .Append(@"namespace DynamicCompile
//{
//    public class DynEval
//    {
//        public DynEval" + ctor +
//@"
//        private async Task<string> Eval<T>(Func<Task<IEnumerable<T>>> set) => String.Join("", "", await set?.Invoke());
//        private async Task<string> Eval<T>(Func<Task<T>> func) => (await func?.Invoke()).ToString() ?? ""null"";
//        private async Task<string> Eval(Func<Task> func) { await func?.Invoke(); return ""Executed""; }
//        public async Task<string> Exec() => await Eval(async () => {expr});
//    }
//}");
//                return new EvalService(_references, sb.ToString());
//            }
//        }
//    }

//    /// <summary> Container for a <see cref="MetadataReference"/> and associated namespaces. </summary>
//    public class EvalReference
//    {
//        /// <summary> The set of namespaces to be imported for evaluation. </summary>
//        public IEnumerable<string> Namespaces { get; }

//        /// <summary> The <see cref="MetadataReference"/> contained in this instance. </summary>
//        public MetadataReference Reference { get; }

//        /// <summary> Creates a new instance of <see cref="EvalReference"/>. </summary>
//        /// <param name="reference">A <see cref="MetadataReference"/>
//        /// pointing to the assembly you wish to reference.</param>
//        /// <param name="namespaces">One or more namespaces defined in the
//        /// referenced assembly to import.</param>
//        public EvalReference(MetadataReference reference, params string[] namespaces)
//        {
//            Reference = reference;
//            Namespaces = namespaces;
//        }
//    }

//    //public static class DynEval
//    //{
//    //    private static async Task<string> Eval<T>(Func<Task<IEnumerable<T>>> set) => String.Join("", "", await set?.Invoke());
//    //    private static async Task<string> Eval<T>(Func<Task<T>> func) => (await func?.Invoke()).ToString() ?? ""null"";
//    //    private static async Task<string> Eval(Func<Task> func) { await func?.Invoke(); return ""Executed""; }
//    //    public static async Task<string> Exec() => await Eval(async () => { expr});
//    //}
//}
