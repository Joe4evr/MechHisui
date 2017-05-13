using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Addons.SimplePermissions;

namespace MechHisui
{
    public sealed class DiceTypeReader : TypeReader
    {
        private static readonly Regex _diceReader = new Regex("[0-9]+d[0-9]+", RegexOptions.Compiled);

        public override Task<TypeReaderResult> Read(ICommandContext context, string input)
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
                dice = dice.Shuffle(28);
                yield return dice.ElementAt(rng.Next(maxValue: Sides));
            }
        }
    }

    [Name("RNG")]
    public sealed class DiceRollModule : ModuleBase<ICommandContext>
    {
        [Command("roll"), Permission(MinimumPermission.Everyone)]
        [Summary("Roll an arbitrary set of arbitrary-sided dice. Uses D&D notation.")]
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

            await ReplyAsync(sb.ToString()).ConfigureAwait(false);
        }

        [Command("pick"), Permission(MinimumPermission.Everyone)]
        public Task PickCmd(params string[] options)
        {
            var realoptions = options.Distinct().ToList();
            if (realoptions.Count < 1)
            {
                return ReplyAsync("Must provide more than one unique option.");
            }
            var choice = realoptions.Shuffle(28).ElementAt(new Random().Next(maxValue: realoptions.Count));
            return ReplyAsync($"**Picked:** `{choice}`");
        }
    }

    public static class DiceRollExtensions
    {
        public static Task AddDiceRoll(this CommandService commands)
        {
            commands.AddTypeReader<DiceRoll>(new DiceTypeReader());
            return commands.AddModuleAsync<DiceRollModule>();
        }
    }
}
