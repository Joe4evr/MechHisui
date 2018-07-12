using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;

namespace MechHisui
{
    public sealed class DiceTypeReader : TypeReader
    {
        private static readonly Regex _diceReader = new Regex("^[0-9]+d[0-9]+$", RegexOptions.Compiled);

        public override Task<TypeReaderResult> ReadAsync(
            ICommandContext context,
            string input,
            IServiceProvider services)
        {
            if (_diceReader.Match(input).Success)
            {
                var splits = input.Split('d');
                if (Int32.TryParse(splits[0], out int amount)
                    && Int32.TryParse(splits[1], out int range)
                    && amount > 0 && range > 0)
                {
                    return Task.FromResult(TypeReaderResult.FromSuccess(new DiceRoll(amount, range)));
                }
            }
            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Invalid format"));
        }
    }
}
