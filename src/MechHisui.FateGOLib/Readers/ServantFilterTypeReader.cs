using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using SharedExtensions;

namespace MechHisui.FateGOLib
{
    public sealed class ServantFilterTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(
            ICommandContext context,
            string input,
            IServiceProvider services)
            => Task.FromResult(TypeReaderResult.FromSuccess(ParseFull(input)));

        public static ServantFilterOptions ParseFull(string input)
        {
            var result = new ServantFilterOptions();
            var span = input.AsSpan().Trim();


            var opWord = span.SliceUntil(' ', out var remainder);
            if (opWord.SequenceEqual(Where.AsSpan()))
            {
                var matchIdx = remainder.FindMatchingBrace();
                var whereClause = remainder.Slice(1, matchIdx - 1);
                remainder = remainder.Slice(matchIdx + 1).Trim();
                result.Predicate = ParseFilters(whereClause);

                opWord = remainder.SliceUntil(' ', out remainder);
            }

            if (opWord.SequenceEqual(OrderBy.AsSpan()))
            {
                var matchIdx = remainder.FindMatchingBrace();
                var orderByClause = remainder.Slice(1, matchIdx - 1);
                remainder = remainder.Slice(matchIdx + 1).Trim();
                result.Order = ParseOrderByClause(orderByClause);

                opWord = remainder.SliceUntil(' ', out remainder);
            }

            if (opWord.SequenceEqual(Select.AsSpan()))
            {
                var matchIdx = remainder.FindMatchingBrace();
                var selectClause = remainder.Slice(1, matchIdx - 1);
                remainder = remainder.Slice(matchIdx + 1).Trim();
                result.Selector = ParseSelectClause(selectClause);
            }

            return result;
        }

        private static readonly string Where = "where";
        private static readonly string OrderBy = "orderby";
        private static readonly string Desc = "d:";
        private static readonly string Select = "select";

        private static readonly string Contains = "<-";
        private static readonly string NotContains = "!<-";
        private static readonly string LessThan = "<";
        private static readonly string LessThanOrEqual = "<=";
        private static readonly string GreaterThan = ">";
        private static readonly string GreaterThanOrEqual = ">=";
        private static readonly string IsEqual = "==";
        private static readonly string IsNotEqual = "!=";

        private static Operator ParseOperator(ReadOnlySpan<char> span)
        {
            if (span.SequenceEqual(Contains.AsSpan()))
                return Operator.Contains;
            if (span.SequenceEqual(NotContains.AsSpan()))
                return Operator.NotContains;
            else if (span.SequenceEqual(LessThan.AsSpan()))
                return Operator.LessThan;
            else if (span.SequenceEqual(LessThanOrEqual.AsSpan()))
                return Operator.LessThanOrEqual;
            else if (span.SequenceEqual(GreaterThan.AsSpan()))
                return Operator.GreaterThan;
            else if (span.SequenceEqual(GreaterThanOrEqual.AsSpan()))
                return Operator.GreaterThanOrEqual;
            else if (span.SequenceEqual(IsEqual.AsSpan()))
                return Operator.IsEqual;
            else if (span.SequenceEqual(IsNotEqual.AsSpan()))
                return Operator.NotEqual;
            else
                throw new InvalidOperationException("Unrecognized operator");
        }

        private static Func<IServantProfile, bool> ParseFilters(ReadOnlySpan<char> filterSpan)
        {
            var resExpr = Expression.Variable(_boolType, "result");
            var filterBlockExpr = Expression.Block(_boolType,
                new[] { resExpr },
                Expression.Assign(resExpr, Expression.Constant(true)));

            for (var slice = filterSpan.SliceUntil(',', out var next); slice.Length > 0; slice = next.SliceUntil(',', out next))
            {
                var prop = slice.SliceUntil(' ', out var a);
                var op = a.SliceUntil(' ', out var val);
                filterBlockExpr = AddClause(filterBlockExpr, resExpr, prop, op, val.TrimBraces());
            }

            var lambda = Expression.Lambda<Func<IServantProfile, bool>>(
                filterBlockExpr,
                _spParamExpr);

            return lambda.Compile();
        }

        private static Func<IEnumerable<IServantProfile>, IOrderedEnumerable<IServantProfile>> ParseOrderByClause(ReadOnlySpan<char> orderBySpan)
        {
            var props = new List<(PropertyInfo, bool)>();

            for (var slice = orderBySpan.SliceUntil(',', out var next); slice.Length > 0; slice = next.SliceUntil(',', out next))
            {
                var (v, d) = (slice.StartsWith(Desc.AsSpan()))
                    ? (slice.Slice(2).Materialize(), true)
                    : (slice.Materialize(), false);
                var prop = _profileProps.SingleOrDefault(p => p.Name.Equals(v, StringComparison.OrdinalIgnoreCase));
                if (prop.PropertyType != _intType)
                    throw new InvalidOperationException("Cannot order by a non-numeric property");

                props.Add((prop, d));
            }

            MethodCallExpression call = null;

            for (int i = 0; i < props.Count; i++)
            {
                var (prop, d) = props[i];
                var propExpr = Expression.Property(_spParamExpr, prop);
                var selExpr = Expression.Lambda<Func<IServantProfile, int>>(propExpr, _spParamExpr);

                if (i == 0)
                    call = Expression.Call(_linqOrderBy,
                        _spsParamExpr,
                        selExpr,
                        Expression.Constant(d));
                else
                    call = Expression.Call(_linqThenBy,
                        call,
                        selExpr,
                        Expression.Constant(d));
            }

            var lambda = Expression.Lambda<Func<IEnumerable<IServantProfile>, IOrderedEnumerable<IServantProfile>>>(call, _spsParamExpr);

            return lambda.Compile();
        }

        private static Func<IServantProfile, string> ParseSelectClause(ReadOnlySpan<char> selectSpan)
        {
            var props = new List<PropertyInfo>();

            for (var slice = selectSpan.SliceUntil(',', out var next); slice.Length > 0; slice = next.SliceUntil(',', out next))
            {
                var v = slice.Materialize();
                var prop = _profileProps.SingleOrDefault(p => p.Name.Equals(v, StringComparison.OrdinalIgnoreCase));
                if (prop != null)
                    props.Add(prop);
            }

            try
            {
                var fmtExpr = Expression.NewArrayInit(_objType, props.Select(p =>
                    Expression.Convert(
                        Expression.Call(
                            _strConcat,
                            Expression.Constant(p.Name),
                            _colonExpr,
                            (p.PropertyType == _strType)
                                ? Expression.Property(_spParamExpr, p)
                                : (Expression)Expression.Call(
                                    Expression.Property(_spParamExpr, p),
                                    _toString)),
                        _objType)));

                var lambda = Expression.Lambda<Func<IServantProfile, string>>(
                    Expression.Call(
                        _strJoin,
                        _commaExpr,
                        fmtExpr),
                    _spParamExpr);

                //options.Selector = lambda.Compile();
                //return FilterParseResult.Success;
                return lambda.Compile();
            }
            catch (Exception)
            {
                throw;
                //return new FilterParseResult(false, "Failed to parse expression");
            }
        }

        private static BlockExpression AddClause(
            BlockExpression blockExpr,
            ParameterExpression resExpr,
            ReadOnlySpan<char> propSpan,
            ReadOnlySpan<char> opSpan,
            ReadOnlySpan<char> valSpan)
        {
            var v = propSpan.Materialize();
            var prop = _profileProps.SingleOrDefault(p => p.Name.Equals(v, StringComparison.OrdinalIgnoreCase));
            var pType = prop.PropertyType;

            var (isCollection, eType) = (pType.IsGenericType && pType.GetGenericTypeDefinition() == _ienumType)
                ? (true, pType.GetGenericArguments()[0])
                : (false, pType);
            var valExpr = (eType == _intType)
                ? Expression.Constant(Convert.ChangeType(valSpan.Materialize(), eType), eType)
                : Expression.Constant(valSpan.Materialize(), _strType);

            var propExpr = Expression.Property(_spParamExpr, prop);
            var intermVarExpr = Expression.Variable(pType, $"interm{blockExpr.Variables.Count}");
            var op = ParseOperator(opSpan);

            if (pType == _intType)
            {
                switch (op)
                {
                    case Operator.LessThan:
                        return blockExpr.Update(
                            blockExpr.Variables.Concat(new[] { intermVarExpr }),
                            blockExpr.Expressions.Concat(
                                new Expression[]
                                {
                                        Expression.Assign(
                                            intermVarExpr,
                                            Expression.Call(propExpr, _intCompare, valExpr)),
                                        Expression.Assign(
                                            resExpr,
                                            Expression.AndAlso(
                                                resExpr,
                                                GenerateExpression(true, intermVarExpr, _intEquals, _intNegOneExpr)))
                                }));
                    case Operator.LessThanOrEqual:
                        return blockExpr.Update(
                            blockExpr.Variables.Concat(new[] { intermVarExpr }),
                            blockExpr.Expressions.Concat(
                                new Expression[]
                                {
                                        Expression.Assign(
                                            intermVarExpr,
                                            Expression.Call(propExpr, _intCompare, valExpr)),
                                        Expression.Assign(
                                            resExpr,
                                            Expression.AndAlso(
                                                resExpr,
                                                Expression.OrElse(
                                                    GenerateExpression(true, intermVarExpr, _intEquals, _intNegOneExpr),
                                                    GenerateExpression(true, intermVarExpr, _intEquals, _intZeroExpr))))
                                }));
                    case Operator.GreaterThan:
                        return blockExpr.Update(
                            blockExpr.Variables.Concat(new[] { intermVarExpr }),
                            blockExpr.Expressions.Concat(
                                new Expression[]
                                {
                                        Expression.Assign(
                                            intermVarExpr,
                                            Expression.Call(propExpr, _intCompare, valExpr)),
                                        Expression.Assign(
                                            resExpr,
                                            Expression.AndAlso(
                                                resExpr,
                                                GenerateExpression(true, intermVarExpr, _intEquals, _intOneExpr)))
                                }));
                    case Operator.GreaterThanOrEqual:
                        return blockExpr.Update(
                            blockExpr.Variables.Concat(new[] { intermVarExpr }),
                            blockExpr.Expressions.Concat(
                                new Expression[]
                                {
                                        Expression.Assign(
                                            intermVarExpr,
                                            Expression.Call(propExpr, _intCompare, valExpr)),
                                        Expression.Assign(
                                            resExpr,
                                            Expression.AndAlso(
                                                resExpr,
                                                Expression.OrElse(
                                                    GenerateExpression(true, intermVarExpr, _intEquals, _intOneExpr),
                                                    GenerateExpression(true, intermVarExpr, _intEquals, _intZeroExpr))))
                                }));
                    case Operator.IsEqual:
                        return blockExpr.Update(
                            blockExpr.Variables,
                            blockExpr.Expressions.Concat(
                                new Expression[]
                                {
                                    Expression.Assign(
                                        resExpr,
                                        Expression.AndAlso(
                                            resExpr,
                                            GenerateExpression(true, propExpr, _intEquals, valExpr)))
                                }));
                    case Operator.NotEqual:
                        return blockExpr.Update(
                            blockExpr.Variables,
                            blockExpr.Expressions.Concat(
                                new Expression[]
                                {
                                    Expression.Assign(
                                        resExpr,
                                        Expression.AndAlso(
                                            resExpr,
                                            GenerateExpression(false, propExpr, _intEquals, valExpr)))
                                }));
                    default:
                        throw new InvalidOperationException($"Operation '{op}' not supported on integers.");
                }
            }
            else if (pType == _strType)
            {
                switch (op)
                {
                    case Operator.IsEqual:
                        return blockExpr.Update(
                            blockExpr.Variables,
                            blockExpr.Expressions.Concat(
                                new Expression[]
                                {
                                    Expression.Assign(
                                        resExpr,
                                        Expression.AndAlso(
                                            resExpr,
                                            GenerateExpression(true, propExpr, _strEquals, valExpr, _strCompExpr)))
                                }));
                    case Operator.NotEqual:
                        return blockExpr.Update(
                            blockExpr.Variables,
                            blockExpr.Expressions.Concat(
                                new Expression[]
                                {
                                    Expression.Assign(
                                        resExpr,
                                        Expression.AndAlso(
                                            resExpr,
                                            GenerateExpression(false, propExpr, _strEquals, valExpr, _strCompExpr)))
                                }));
                    default:
                        throw new InvalidOperationException($"Operation '{op}' not supported on strings.");
                }
            }
            else if (isCollection)
            {
                switch (op)
                {
                    case Operator.Contains:
                        return blockExpr.Update(
                            blockExpr.Variables,
                            blockExpr.Expressions.Concat(
                                new Expression[]
                                {
                                    Expression.Assign(
                                        resExpr,
                                        Expression.AndAlso(
                                            resExpr,
                                            GenerateExpression(true, null, GetContains(eType), propExpr, valExpr)))
                                }));
                    case Operator.NotContains:
                        return blockExpr.Update(
                            blockExpr.Variables,
                            blockExpr.Expressions.Concat(
                                new Expression[]
                                {
                                    Expression.Assign(
                                        resExpr,
                                        Expression.AndAlso(
                                            resExpr,
                                            GenerateExpression(false, null, GetContains(eType), propExpr, valExpr)))
                                }));
                    default:
                        throw new InvalidOperationException($"Operation '{op}' not supported on a collections.");
                }
            }
            else
            {
                throw new InvalidOperationException($"Property type '{pType}' not supported.");
            }



            Expression GenerateExpression(
                bool isTrue,
                Expression target,
                MethodInfo method,
                params Expression[] args)
            {
                var call = (method.IsStatic)
                    ? Expression.Call(method, args)
                    : Expression.Call(target, method, args);
                return (isTrue) ? Expression.IsTrue(call) : Expression.IsFalse(call);
            }
        }

        private static readonly Type _objType = typeof(object);
        private static readonly Type _strType = typeof(string);
        private static readonly Type _boolType = typeof(bool);
        private static readonly Type _intType = typeof(int);
        private static readonly Type _linqType = typeof(Enumerable);
        private static readonly Type _ienumType = typeof(IEnumerable<>);
        private static readonly Type _spType = typeof(IServantProfile);
        private static readonly Type _ienumSpType = typeof(IEnumerable<IServantProfile>);
        private static readonly Type _iOrdEnumSpType = typeof(IOrderedEnumerable<IServantProfile>);
        private static readonly Type _funcSpToIntType = typeof(Func<IServantProfile, int>);
        private static readonly Type _exType = typeof(Ex);

        private static readonly PropertyInfo[] _profileProps = _spType.GetProperties();
        private static readonly Type[] _strTypeArr = new Type[] { _strType };
        private static readonly Type[] _strCompTypeArr = new Type[] { _strType, typeof(StringComparison) };
        private static readonly Type[] _intTypeArr = new Type[] { _intType };

        private static readonly MethodInfo _toString = _objType.GetMethod("ToString");
        private static readonly MethodInfo _strJoin = _strType.GetMethod("Join", new Type[] { _strType, typeof(object[]) });
        private static readonly MethodInfo _strContains = _strType.GetMethod("Contains", _strTypeArr);
        private static readonly MethodInfo _strConcat = _strType.GetMethod("Concat", new Type[] { _strType, _strType, _strType });
        private static readonly MethodInfo _strEquals = _strType.GetMethod("Equals", _strCompTypeArr);
        private static readonly MethodInfo _intEquals = _intType.GetMethod("Equals", _intTypeArr);
        private static readonly MethodInfo _intCompare = _intType.GetMethod("CompareTo", _intTypeArr);
        private static readonly MethodInfo _linqOrderBy = _exType.GetMethod("Order", new Type[] { _ienumSpType, _funcSpToIntType, _boolType });
        private static readonly MethodInfo _linqThenBy = _exType.GetMethod("ThenOrder", new Type[] { _iOrdEnumSpType, _funcSpToIntType, _boolType });

        private static readonly ParameterExpression _spParamExpr = Expression.Parameter(_spType, "servant");
        private static readonly ParameterExpression _spsParamExpr = Expression.Parameter(_ienumSpType, "servants");
        private static readonly ConstantExpression _commaExpr = Expression.Constant(", ");
        private static readonly ConstantExpression _colonExpr = Expression.Constant(": ");
        private static readonly ConstantExpression _strCompExpr = Expression.Constant(StringComparison.OrdinalIgnoreCase);
        private static readonly ConstantExpression _intNegOneExpr = Expression.Constant(-1);
        private static readonly ConstantExpression _intZeroExpr = Expression.Constant(0);
        private static readonly ConstantExpression _intOneExpr = Expression.Constant(1);

        private static MethodInfo GetContains(Type elemType)
            => _exType.GetMethod("Contains", new Type[] { _ienumType.MakeGenericType(new Type[] { elemType }), _strType });

        private enum Operator
        {
            Contains,
            NotContains,
            LessThan,
            LessThanOrEqual,
            GreaterThan,
            GreaterThanOrEqual,
            IsEqual,
            NotEqual
        }
    }

    internal static class Ex
    {
        public static bool Contains(IEnumerable<IServantTrait> traits, string trait)
            => traits.Any(t => t.Trait.Equals(trait, StringComparison.OrdinalIgnoreCase));
        public static bool Contains(IEnumerable<IServantAlias> aliases, string alias)
            => aliases.Any(a => a.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase));
        public static bool Contains(IEnumerable<IActiveSkill> skills, string skill)
            => skills.Any(a => a.SkillName.Equals(skill, StringComparison.OrdinalIgnoreCase));
        public static bool Contains(IEnumerable<IPassiveSkill> skills, string skill)
            => skills.Any(s => s.SkillName.Equals(skill, StringComparison.OrdinalIgnoreCase));
        public static IOrderedEnumerable<IServantProfile> Order(IEnumerable<IServantProfile> profiles, Func<IServantProfile, int> selector, bool descending)
            => (descending)
                ? profiles.OrderByDescending(selector)
                : profiles.OrderBy(selector);
        public static IOrderedEnumerable<IServantProfile> ThenOrder(IOrderedEnumerable<IServantProfile> profiles, Func<IServantProfile, int> selector, bool descending)
            => (descending)
                ? profiles.ThenByDescending(selector)
                : profiles.ThenBy(selector);
    }
}
