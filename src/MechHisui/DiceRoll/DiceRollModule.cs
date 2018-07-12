using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Addons.SimplePermissions;
using SharedExtensions;

namespace MechHisui
{
    [Name("RNG")]
    public sealed class DiceRollModule : ModuleBase<ICommandContext>
    {
        private readonly Random _rng;

        public DiceRollModule(Random rng)
        {
            _rng = rng;
        }

        [Command("roll"), Permission(MinimumPermission.Everyone)]
        [Summary("Roll an arbitrary set of arbitrary-sided dice. Uses D&D notation.")]
        public Task DiceRoll(params DiceRoll[] dice)
        {
            var rolls = new List<int>();
            var sb = new StringBuilder("**Rolled: **")
                .AppendSequence(dice, (b, d) =>
                {
                    var t = d.Roll(_rng).ToList();
                    rolls.AddRange(t);
                    return b.Append($"({String.Join(", ", t)}{(t.Count > 1 ? $" | sum: {t.Sum()}" : "")})");
                })
                .AppendWhen(rolls.Count > 1, b => b.Append($"\n(Total sum: {rolls.Sum()})"));

            return ReplyAsync(sb.ToString());
        }

        [Command("pick"), Permission(MinimumPermission.Everyone)]
        [Summary("🎵RNG, RNG, Please be nice to me....🎵")]
        public Task PickCmd(params string[] options)
        {
            var realoptions = options.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (realoptions.Count <= 1)
            {
                return ReplyAsync("Must provide more than one unique option.");
            }
            var choice = realoptions.Shuffle(28).ElementAt(_rng.Next(maxValue: realoptions.Count));
            return ReplyAsync($"**Picked:** `{choice}`");
        }
    }

    //public static class DiceRollExtensions
    //{
    //    public static Task AddDiceRoll(this CommandService commands)
    //    {
    //        commands.AddTypeReader<DiceRoll>(new DiceTypeReader());
    //        return commands.AddModuleAsync<DiceRollModule>();
    //    }
    //}
}
