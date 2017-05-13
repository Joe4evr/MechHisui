using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace MechHisui.HisuiBets
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal class LimitToAttribute : ParameterPreconditionAttribute
    {
        private readonly IEnumerable<string> _options;
        private readonly StringComparison _comparison;

        public LimitToAttribute(StringComparison comparison, params string[] options)
        {
            _comparison = comparison;
            _options = options;
        }

        public override Task<PreconditionResult> CheckPermissions(
            ICommandContext context,
            ParameterInfo parameter,
            object value,
            IServiceProvider services)
        {
            return (value is string str && _options.Contains(str, GetComparer()))
                ? Task.FromResult(PreconditionResult.FromSuccess())
                : Task.FromResult(PreconditionResult.FromError("Invalid parameter value."));
        }

        private StringComparer GetComparer()
        {
            switch (_comparison)
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
