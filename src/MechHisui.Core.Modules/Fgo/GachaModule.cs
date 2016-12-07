using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using JiiLib;
using Discord.Commands;
using Discord.Addons.SimplePermissions;

namespace MechHisui.FateGOLib.Modules
{
    public sealed class GachaModule : ModuleBase
    {
        private static readonly string[] rolltypes = new[] { "fp1", "fp10", "ticket", "3q", "30q" };

        private static readonly string[] fpOnly = new[]
        {
            "Azoth Blade",
            "Book of the False Attendant",
            "Blue Black Keys",
            "Green Black Keys",
            "Red Black Keys",
            "Rin's Pendant",
            "Grimoire",
            "Leyline",
            "Magic Crystal",
            "Dragonkin",
        };

        private readonly StatService _statService;

        public GachaModule(StatService statService)
        {
            _statService = statService;
        }

        [Command("gacha"), Permission(MinimumPermission.Everyone)]
        [Summary(@"Simulate gacha roll (not accurate wrt rarity ratios and rate ups).
Acceptable parameters are `fp1`, `fp10`, `ticket`, `3q`, `30q`")]
        public async Task GachaCmd(string rolltype)
        {
            if (!rolltypes.Contains(rolltype))
            {
                await ReplyAsync("Unacceptable parameter. Use `.help gacha` to see the acceptable values.");
                return;
            }

            var rng = new Random();
            IEnumerable<string> pool = (rolltype == rolltypes[0] || rolltype == rolltypes[1]) ?
                fpPool().ToList() :
                premiumPool().ToList();

            List<string> picks = new List<string>();

            for (int i = 0; i < 28; i++)
            {
                pool = pool.Shuffle();
            }

            if (rolltype == rolltypes[0] || rolltype == rolltypes[2] || rolltype == rolltypes[3])
            {
                pool = pool.Shuffle();
                picks.Add(pool.ElementAt(rng.Next(maxValue: pool.Count())));
            }
            else //10-roll
            {
                for (int i = 0; i < 10; i++)
                {
                    pool = pool.Shuffle();
                    picks.Add(pool.ElementAt(rng.Next(maxValue: pool.Count())));
                }
            }

            await ReplyAsync($"**{Context.User.Username} rolled:** {String.Join(", ", picks)}");
        }

        private static IEnumerable<string> premiumPool() => FgoHelpers.ServantProfiles
            .Where(p => p.Rarity >= 3)
            .Concat(FgoHelpers.ServantProfiles
                .Where(p => p.Rarity >= 3 && p.Rarity <= 4)
                .RepeatSeq(5))
            .Concat(FgoHelpers.ServantProfiles
                .Where(p => p.Rarity == 3)
                .RepeatSeq(5))
            .Where(p => p.Obtainable)
            .Select(p => p.Name)
            .Concat(FgoHelpers.CEProfiles
                .Where(ce => ce.Rarity >= 3)
                .Concat(FgoHelpers.CEProfiles
                    .Where(ce => ce.Rarity >= 3 && ce.Rarity <= 4)
                    .RepeatSeq(5))
                .Concat(FgoHelpers.CEProfiles
                    .Where(ce => ce.Rarity == 3)
                    .RepeatSeq(5))
                .Where(ce => ce.Obtainable)
                .Select(ce => ce.Name));

        private static IEnumerable<string> fpPool() => FgoHelpers.ServantProfiles
            .Where(p => p.Rarity <= 3 && p.Rarity > 0)
            .Concat(FgoHelpers.ServantProfiles
                .Where(p => p.Rarity <= 2 && p.Rarity > 0)
                .RepeatSeq(5))
            .Concat(FgoHelpers.ServantProfiles
                .Where(p => p.Rarity == 1)
                .RepeatSeq(5))
            .Concat(FgoHelpers.ServantProfiles.Where(p => p.Rarity == 0))
            .Where(p => p.Obtainable)
            .Select(p => p.Name)
            .Concat(FgoHelpers.CEProfiles
                .Where(ce => ce.Rarity <= 3)
                .Concat(FgoHelpers.CEProfiles
                    .Where(ce => ce.Rarity <= 2)
                    .RepeatSeq(5))
                .Concat(FgoHelpers.CEProfiles
                    .Where(ce => ce.Rarity == 1)
                    .RepeatSeq(5))
                .Where(ce => ce.Obtainable)
                .Select(ce => ce.Name))
            .Concat(fpOnly.RepeatSeq(3));
    }
}
