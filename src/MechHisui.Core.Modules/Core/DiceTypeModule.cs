using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JiiLib;
using Discord.Commands;
using Discord.Addons.SimplePermissions;

namespace MechHisui.Core.Modules
{
    public sealed class DiceTypeReader : TypeReader
    {
        private static readonly Regex diceReader = new Regex("[0-9]+d[0-9]+", RegexOptions.Compiled);
        public override Task<TypeReaderResult> Read(CommandContext context, string input)
        {
            if (diceReader.Match(input).Success)
            {
                var splits = input.Split('d');
                int amount;
                int range;
                if (Int32.TryParse(splits[0], out amount) &&
                    Int32.TryParse(splits[1], out range) &&
                    amount > 0 && range > 0)
                {
                    return Task.FromResult(TypeReaderResult.FromSuccess(new DiceRoll(amount, range)));
                }
            }
            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Invalid format"));
        }
    }

    public sealed class DiceRoll
    {
        public int Amount { get; }
        public int Sides { get; }

        public DiceRoll(int amount, int sides)
        {
            Amount = amount;
            Sides = sides;
        }

        public IEnumerable<int> Roll()
        {
            var rng = new Random();
            var dice = Enumerable.Range(1, Sides);
            for (int i = 0; i < Amount; i++)
            {
                for (int j = 0; j < 28; j++)
                {
                    dice = dice.Shuffle();
                }

                yield return dice.ElementAt(rng.Next(maxValue: Sides));
            }
        }
    }

    [Name("Dice")]
    public sealed class DiceRollModule : ModuleBase
    {
        [Command("roll"), Permission(MinimumPermission.Everyone)]
        [Summary("Roll an arbitrary set of arbitrary-sided dice. Takes D&D notation.")]
        public async Task DiceRoll(params DiceRoll[] dice)
        {
            var rolls = new List<int>();
            var sb = new StringBuilder("**Rolled: **")
                .AppendSequence(dice, (b, d) =>
                {
                    var t = d.Roll().ToList();
                    rolls.AddRange(t);
                    return b.Append($"({String.Join(", ", t)}{(t.Count > 1 ? $" | sum: {t.Sum()}" : "")})");
                })
                .AppendWhen(() => rolls.Count > 1, b => b.Append($"\n(Total sum: {rolls.Sum()})"));

            await ReplyAsync(sb.ToString());
        }
    }

    //public sealed class DiceRollService
    //{
    //    public DiceRollService(CommandService cService)
    //    {
    //        cService.AddTypeReader<DiceRoll>(new DiceTypeReader());
    //    }
    //}
}
