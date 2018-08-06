using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace MechHisui.SecretHitler
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class LimitRangeAttribute : ParameterPreconditionAttribute
    {
        private readonly int _low;
        private readonly int _high;

        [DebuggerStepThrough]
        public LimitRangeAttribute(int low, int high)
        {
            _low = low;
            _high = high;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context,
            ParameterInfo parameter,
            object value,
            IServiceProvider services)
        {
            return (value is int i && _low <= i && i <= _high)
                ? Task.FromResult(PreconditionResult.FromSuccess())
                : Task.FromResult(PreconditionResult.FromError($"Argument out of range. Value must be between `{_low}` and `{_high}`."));
        }
    }
}
