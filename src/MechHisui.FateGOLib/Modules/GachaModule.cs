using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using JiiLib;
using Discord;
using Discord.Commands;
using Discord.Modules;

namespace MechHisui.FateGOLib.Modules
{
    public class GachaModule : IModule
    {
        private readonly StatService _statService;
        private readonly IConfiguration _config;

        public GachaModule(StatService statService, IConfiguration config)
        {
            _statService = statService;
            _config = config;
        }

        void IModule.Install(ModuleManager manager)
        {
            Console.WriteLine("Registering 'Gacha'...");
            manager.Client.GetService<CommandService>().CreateCommand("gacha")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(_config["FGO_playground"]))
                .Description($"Simulate gacha roll (not accurate wrt rarity ratios and rate ups). Accepetable parameters are `{String.Join("`, `", rolltypes)}`")
                .Parameter("type", ParameterType.Optional)
                .Do(async cea =>
                {
                    //await cea.Channel.SendMessage("This command temporarily disabled.");
                    if (!rolltypes.Contains(cea.Args[0]))
                    {
                        await cea.Channel.SendMessage("Unaccaptable parameter. Use `.help gacha` to see the accaptable values.");
                        return;
                    }

                    var rng = new Random();
                    IEnumerable<string> pool = (cea.Args[0] == rolltypes[0] || cea.Args[0] == rolltypes[1]) ? fpPool.ToList() : premiumPool.ToList();
                    List<string> picks = new List<string>();

                    for (int i = 0; i < 28; i++)
                    {
                        pool = pool.Shuffle();
                    }

                    if (cea.Args[0] == rolltypes[0] || cea.Args[0] == rolltypes[2] || cea.Args[0] == rolltypes[3])
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

                    await cea.Channel.SendMessage($"**{cea.User.Name} rolled:** {String.Join(", ", picks)}");
                });
        }

        private static readonly string[] rolltypes = new[] { "fp1", "fp10", "ticket", "4q", "40q" };

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

        private static readonly IEnumerable<string> premiumPool = FgoHelpers.ServantProfiles
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

        private static readonly IEnumerable<string> fpPool = FgoHelpers.ServantProfiles
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
