using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;

namespace MechHisui.Core
{
    public sealed class DiceTypeReader : TypeReader
    {
        private static readonly Regex _diceReader = new Regex("^-?[0-9]+d[0-9]+$", RegexOptions.Compiled);

        public override Task<TypeReaderResult> ReadAsync(
            ICommandContext context,
            string input,
            IServiceProvider services)
        {
            if (_diceReader.Match(input).Success)
            {
                var splits = input.Split('d');
                var a = splits[0];
                var r = splits[1];
                bool isNeg = a.StartsWith("-");
                if (Int32.TryParse((isNeg ? a.Substring(1) : a), out int amount)
                    && Int32.TryParse(r, out int range)
                    && amount > 0 && range > 0)
                {
                    return Task.FromResult(TypeReaderResult.FromSuccess(new DiceRoll(amount, range, isNeg)));
                }
            }
            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Invalid format"));
        }
    }
}
