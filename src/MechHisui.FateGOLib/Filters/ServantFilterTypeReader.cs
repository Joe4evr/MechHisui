//using System;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Reflection;
//using System.Threading.Tasks;
//using Discord.Commands;

//namespace MechHisui.FateGOLib
//{
//    public sealed partial class ServantFilterTypeReader : TypeReader
//    {
//        public override Task<TypeReaderResult> Read(
//            ICommandContext context,
//            string input,
//            IServiceProvider services)
//        {
//            var result = new ServantFilterOptions();
//            var span = input.AsReadOnlySpan();

//            for (var slice = span.SliceUntil(',', out var next); next.Length > 0; slice = next.SliceUntil(',', out next))
//            {
//                var p = ParseSlice(slice, result);
//                if (!p.IsSuccess)
//                    return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, p.ErrorReason));
//            }

//            return Task.FromResult(TypeReaderResult.FromSuccess(result));
//        }

//        private static readonly string LessThan    = "<";
//        private static readonly string GreaterThan = ">";
//        private static readonly string IsEqual     = "==";
//        private static readonly string IsNotEqual  = "!=";

//        private static FilterParseResult ParseSlice(ReadOnlySpan<char> text, ServantFilterOptions options)
//        {
//            var prop = text.SliceUntil(' ', out var opspan).Materialize();
//            var op = ParseOperator(opspan.SliceUntil(' ', out var value));

//            return (FilterFuncs.TryGetValue(prop, out var parser))
//                ? parser(prop, op, value, options)
//                : FilterParseResult.UnexpectedExpr;
//        }

//        private static readonly PropertyInfo[] _profileProps = typeof(IServantProfile).GetProperties();

//        //private static string ParseProperty(ReadOnlySpan<char> span)
//        //{
//        //    return "";
//        //}

//        private static Operator ParseOperator(ReadOnlySpan<char> span)
//        {
//            if (span.SequenceEqual(LessThan.AsReadOnlySpan()))
//                return Operator.LessThan;
//            else if (span.SequenceEqual(GreaterThan.AsReadOnlySpan()))
//                return Operator.GreaterThan;
//            else if (span.SequenceEqual(IsEqual.AsReadOnlySpan()))
//                return Operator.IsEqual;
//            else if (span.SequenceEqual(IsNotEqual.AsReadOnlySpan()))
//                return Operator.NotEqual;
//            else
//                throw new Exception("Unrecognized operator");
//        }

//        //private static string ParseValue(ReadOnlySpan<char> span)
//        //{
//        //    return "";
//        //}

//        private enum Operator
//        {
//            LessThan,
//            GreaterThan,
//            IsEqual,
//            NotEqual
//        }

//        private delegate FilterParseResult PredicateParser(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options);

//        private static IReadOnlyDictionary<string, PredicateParser> FilterFuncs { get; }
//            = new Dictionary<string, PredicateParser>(StringComparer.OrdinalIgnoreCase)
//            {
//                [nameof(IServantProfile.Id)] = ParseIdPredicate,
//                [nameof(IServantProfile.Class)] = ParseClassPredicate,
//                [nameof(IServantProfile.Rarity)] = ParseRarityPredicate,
//                [nameof(IServantProfile.Name)] = ParseNamePredicate,
//                [nameof(IServantProfile.Atk)] = ParseAtkPredicate,
//                [nameof(IServantProfile.HP)] = ParseHPPredicate,
//                [nameof(IServantProfile.Starweight)] = ParseStarweightPredicate,
//                [nameof(IServantProfile.Gender)] = ParseGenderPredicate,
//                [nameof(IServantProfile.Attribute)] = ParseAttributePredicate,
//                [nameof(IServantProfile.GrowthCurve)] = ParseGrowthCurvePredicate,

//                //[nameof(IServantProfile.CardPool)] = ParseCardPoolPredicate,
//                //[nameof(IServantProfile.B)] = ParseBusterPredicate,
//                //[nameof(IServantProfile.A)] = ParseArtsPredicate,
//                //[nameof(IServantProfile.Q)] = ParseQuickPredicate,
//                //[nameof(IServantProfile.EX)] = ParseEXPredicate,

//                //[nameof(IServantProfile.NPType)] = ParseNPTypePredicate,
//                //[nameof(IServantProfile.NoblePhantasm)] = ParseNoblePhantasmPredicate,
//                //[nameof(IServantProfile.NoblePhantasmEffect)] = ParseNPEffectPredicate,

//                ////[nameof(IServantProfile.Obtainable)] = ParseObtainablePredicate,
//                //[nameof(IServantProfile.Traits)] = ParseTraitsPredicate,
//                //[nameof(IServantProfile.ActiveSkills)] = ParseActiveSkillsPredicate,
//                //[nameof(IServantProfile.PassiveSkills)] = ParsePassiveSkillsPredicate,
//            };
//    }

//    internal static class Ex
//    {
//        //public static IReadOnlyList<ReadOnlyMemory<char>> Split(this ReadOnlySpan<char> span, char[] seperator, StringSplitOptions options)
//        //{
//        //    var temp = new List<ReadOnlyMemory<char>>();
//        //    int lastSlice = 0;
//        //    for (int i = 0; i < span.Length; i++)
//        //    {
//        //        if (seperator.Contains(span[i]))
//        //        {
//        //            if (!(options == StringSplitOptions.RemoveEmptyEntries && i == lastSlice))
//        //            {
//        //                temp.Add(span.Slice(lastSlice, i - lastSlice).AsMemory());
//        //                lastSlice = i += 1;
//        //            }
//        //        }
//        //    }

//        //    return temp.ToImmutableArray();
//        //}

//        public static ReadOnlySpan<char> SliceUntil(this ReadOnlySpan<char> span, char delimeter, out ReadOnlySpan<char> remainder)
//        {
//            for (int i = 0; i < span.Length; i++)
//            {
//                if (span[i] == delimeter && i > 0)
//                {
//                    int remStart = i + 1; //skip the delimeter
//                    remainder = span.Slice(remStart, span.Length - remStart);
//                    return span.Slice(0, i);
//                }
//            }
//            remainder = ReadOnlySpan<char>.Empty;
//            return span;
//        }

//        public static string Materialize(this ReadOnlySpan<char> span) => new string(span.ToArray());
//    }
//}
