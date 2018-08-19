using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using JiiLib.SimpleDsl;
using SharedExtensions;

namespace MechHisui.FateGOLib
{
    public partial class FgoModule
    {
        private sealed class ServantFilterTypeReader : TypeReader
        {
            private static readonly QueryInterpreter<IServantProfile> _interpreter = new QueryInterpreter<IServantProfile>(ServantInterpreterConfig.Instance);

            public override Task<TypeReaderResult> ReadAsync(
                ICommandContext context,
                string input,
                IServiceProvider services)
            {
                try
                {
                    var result = _interpreter.ParseFull(input);
                    return Task.FromResult(TypeReaderResult.FromSuccess(result));
                }
                catch (Exception ex)
                {
                    return Task.FromResult(TypeReaderResult.FromError(CommandError.Exception, ex.Message));
                }
            }

            private sealed class ServantInterpreterConfig : IInterpreterConfig<IServantProfile>
            {
                public static IInterpreterConfig<IServantProfile> Instance { get; } = new ServantInterpreterConfig();

                private static readonly Type _strType = typeof(string);
                private static readonly Type _boolType = typeof(bool);
                private static readonly Type _thisType = typeof(ServantInterpreterConfig);
                private static readonly Type _spType = typeof(IServantProfile);
                private static readonly Type _ienumSpType = typeof(IEnumerable<IServantProfile>);
                private static readonly Type _iOrdEnumSpType = typeof(IOrderedEnumerable<IServantProfile>);
                private static readonly Type _funcSpToIntType = typeof(Func<IServantProfile, int>);

                private static readonly IReadOnlyDictionary<Type, MethodInfo> _containsMethods
                    = new Dictionary<Type, MethodInfo>
                    {
                        [typeof(IServantAlias)] = _thisType.GetMethod("Contains", new Type[] { typeof(IEnumerable<IServantAlias>), _strType }),
                        [typeof(IActiveSkill)] = _thisType.GetMethod("Contains", new Type[] { typeof(IEnumerable<IActiveSkill>), _strType }),
                        [typeof(IPassiveSkill)] = _thisType.GetMethod("Contains", new Type[] { typeof(IEnumerable<IPassiveSkill>), _strType })
                    }.ToImmutableDictionary();
                private static readonly IReadOnlyDictionary<(bool, bool), MethodInfo> _orderBys
                    = new Dictionary<(bool first, bool descending), MethodInfo>
                    {
                        [(true, false)] = _thisType.GetMethod("OrderBy", new Type[] { _ienumSpType, _funcSpToIntType }),
                        [(true, true)] = _thisType.GetMethod("OrderByDescending", new Type[] { _ienumSpType, _funcSpToIntType }),
                        [(false, false)] = _thisType.GetMethod("ThenOrderBy", new Type[] { _iOrdEnumSpType, _funcSpToIntType }),
                        [(false, true)] = _thisType.GetMethod("ThenOrderByDescending", new Type[] { _iOrdEnumSpType, _funcSpToIntType })
                    }.ToImmutableDictionary();

                private ServantInterpreterConfig() { }

                string IInterpreterConfig<IServantProfile>.FormatString(string value, FormatModifiers formats)
                {
                    if ((formats & FormatModifiers.Bold) == FormatModifiers.Bold)
                        value = $"**{value}**";

                    if ((formats & FormatModifiers.Italic) == FormatModifiers.Italic)
                        value = $"*{value}*";

                    return value;
                }

                MethodInfo IInterpreterConfig<IServantProfile>.GetContainsMethod(Type elementType)
                {
                    if (_containsMethods.TryGetValue(elementType, out var method))
                        return method;
                    throw new InvalidOperationException($"Contains operation not supported on type '{elementType}'");
                }

                MethodInfo IInterpreterConfig<IServantProfile>.GetOrderByMethod(Type elementType, bool descending)
                {
                    if (elementType == _spType
                        && _orderBys.TryGetValue((true, descending), out var method))
                        return method;
                    throw new InvalidOperationException();
                }

                MethodInfo IInterpreterConfig<IServantProfile>.GetThenByMethod(Type elementType, bool descending)
                {
                    if (elementType == _spType
                        && _orderBys.TryGetValue((false, descending), out var method))
                        return method;
                    throw new InvalidOperationException();
                }

                public static bool Contains(IEnumerable<IServantAlias> aliases, string alias)
                    => aliases.Any(a => a.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase));
                public static bool Contains(IEnumerable<IActiveSkill> skills, string skill)
                    => skills.Any(a => a.Name.Equals(skill, StringComparison.OrdinalIgnoreCase));
                public static bool Contains(IEnumerable<IPassiveSkill> skills, string skill)
                    => skills.Any(s => s.Name.Equals(skill, StringComparison.OrdinalIgnoreCase));
                public static IOrderedEnumerable<IServantProfile> OrderBy(IEnumerable<IServantProfile> profiles, Func<IServantProfile, int> selector)
                    => profiles.OrderBy(selector);
                public static IOrderedEnumerable<IServantProfile> OrderByDescending(IEnumerable<IServantProfile> profiles, Func<IServantProfile, int> selector)
                    => profiles.OrderByDescending(selector);
                public static IOrderedEnumerable<IServantProfile> ThenOrderBy(IOrderedEnumerable<IServantProfile> profiles, Func<IServantProfile, int> selector)
                    => profiles.ThenBy(selector);
                public static IOrderedEnumerable<IServantProfile> ThenOrderByDescending(IOrderedEnumerable<IServantProfile> profiles, Func<IServantProfile, int> selector)
                    => profiles.ThenByDescending(selector);
            }
        }
    }
}
