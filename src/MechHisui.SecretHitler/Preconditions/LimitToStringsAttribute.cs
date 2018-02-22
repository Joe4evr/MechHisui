using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace MechHisui.SecretHitler
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class LimitToStringsAttribute : ParameterPreconditionAttribute
    {
        private readonly IReadOnlyCollection<string> _options;
        private readonly StringComparer _comparer;

        public LimitToStringsAttribute(StringComparison comparison, params string[] options)
        {
            _comparer = GetComparer(comparison);
            _options = options.ToImmutableArray();
        }

        public override Task<PreconditionResult> CheckPermissions(
            ICommandContext context,
            ParameterInfo parameter,
            object value,
            IServiceProvider services)
        {
            return (value is string str && _options.Contains(str, _comparer))
                ? Task.FromResult(PreconditionResult.FromSuccess())
                : Task.FromResult(PreconditionResult.FromError($"Invalid parameter value. Valid values are `{String.Join("`, `", _options)}`."));
        }

        private static StringComparer GetComparer(StringComparison comp)
        {
            switch (comp)
            {
                case StringComparison.CurrentCulture:
                    return StringComparer.CurrentCulture;

                case StringComparison.CurrentCultureIgnoreCase:
                    return StringComparer.CurrentCultureIgnoreCase;

                case StringComparison.Ordinal:
                    return StringComparer.Ordinal;

                case StringComparison.OrdinalIgnoreCase:
                default:
                    return StringComparer.OrdinalIgnoreCase;
            }
        }
    }
}
