using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Addons.SimplePermissions;
using SharedExtensions;
using Discord.Commands.Builders;

namespace MechHisui.Core
{
    [Name("RNG")/*, Permission(MinimumPermission.Everyone)*/]
    public sealed class DiceRollModule : ModuleBase<ICommandContext>
    {
        private readonly Random _rng;

        public DiceRollModule(Random rng)
        {
            _rng = rng;
        }

        protected override void OnModuleBuilding(CommandService commandService, ModuleBuilder builder)
        {
            base.OnModuleBuilding(commandService, builder);

            commandService.AddTypeReader<DiceRoll>(new DiceTypeReader());
        }

        [Command("roll")]
        [Summary("Roll an arbitrary set of arbitrary-sided dice. Uses D&D notation.")]
        public Task DiceRoll(params DiceRoll[] dice)
        {
            var sums = new List<int>();
            var sb = new StringBuilder("**Rolled: **")
                .AppendSequence(dice, (b, d) =>
                {
                    var t = d.Roll(_rng).ToList();
                    var sum = t.Sum();
                    sums.Add((d.IsNegative ? -sum : sum));
                    return b.Append($"{(d.IsNegative ? "-" : "")}({String.Join(", ", t)}{(t.Count > 1 ? $" | sum: {sum}" : "")}) ");
                })
                .AppendWhen(sums.Count > 1, b => b.Append($"\n(Total: {sums.Sum()})"));

            return ReplyAsync(sb.ToString());
        }

        [Command("pick")]
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
}
