﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using JiiLib;
using Discord;
using Discord.Commands;
using Discord.Modules;

namespace MechHisui.FateGOLib.Modules
{
    public class ServantStatsModule : IModule
    {
        private readonly StatService _statService;
        private readonly IConfiguration _config;

        public ServantStatsModule(StatService statService, IConfiguration config)
        {
            _statService = statService;
            _config = config;
        }

        void IModule.Install(ModuleManager manager)
        {
            Console.WriteLine("Registering 'Stats'...");
            manager.Client.GetService<CommandService>().CreateCommand("servant")
                .Alias("stat", "stats")
                .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(_config["FGO_server"]))
                .Parameter("name", ParameterType.Unparsed)
                .Description($"Relay information on the specified Servant. Alternative names acceptable.")
                .Do(async cea =>
                {
                    if (String.IsNullOrWhiteSpace(cea.Args[0]))
                    {
                        return;
                    }

                    if (cea.Args[0].ContainsIgnoreCase("waifu"))
                    {
                        await cea.Channel.SendWithRetry("It has come to my attention that your 'waifu' is equatable to fecal matter.");
                        return;
                    }

                    if (new[] { "enkidu", "arc", "arcueid" }.ContainsIgnoreCase(cea.Args[0]))
                    {
                        await cea.Channel.SendWithRetry("Never ever.");
                        return;
                    }
                    
                    int id;
                    if (Int32.TryParse(cea.Args[0], out id))
                    {
                        var profile = FgoHelpers.ServantProfiles.SingleOrDefault(p => p.Id == id) ??
                            FgoHelpers.FakeServantProfiles.SingleOrDefault(p => p.Id == id);

                        await cea.Channel.SendWithRetry(FormatServantProfile(profile));
                    }
                    else
                    {
                        var potentials = _statService.LookupStats(cea.Args[0]);
                        if (potentials.Count() == 1)
                        {
                            await cea.Channel.SendWithRetry(FormatServantProfile(potentials.Single()));
                        }
                        else if (potentials.Count() > 1)
                        {
                            var sb = new StringBuilder("Entry ambiguous. Did you mean one of the following?\n")
                                .AppendSequence(potentials, (s, pr) => s.AppendLine($"**{pr.Name}** *({String.Join(", ", FgoHelpers.ServantDict.Where(d => d.Value == pr.Name).Select(d => d.Key))})*"));

                            await cea.Channel.SendWithRetry(sb.ToString());
                        }
                        else
                        {
                            await cea.Channel.SendWithRetry("No such entry found. Please try another name.");
                        }
                    }
                });

            Console.WriteLine("Registering 'Add alias'...");
            manager.Client.GetService<CommandService>().CreateCommand("addalias")
                .AddCheck((c, u, ch) => u.Roles.Select(r => r.Id).Contains(UInt64.Parse(_config["FGO_Admins"])) && ch.Server.Id == UInt64.Parse(_config["FGO_server"]))
                .Hide()
                .Parameter("servant", ParameterType.Required)
                .Parameter("alias", ParameterType.Required)
                .Do(async cea =>
                {
                    var servant = cea.Args[0];
                    if (!FgoHelpers.ServantProfiles.Select(p => p.Name).Contains(servant))
                    {
                        await cea.Channel.SendWithRetry("Could not find name to add alias for.");
                        return;
                    }

                    var alias = cea.Args[1].ToLowerInvariant();
                    try
                    {
                        FgoHelpers.ServantDict.Add(alias, servant);
                        File.WriteAllText(Path.Combine(_config["AliasPath"], "servants.json"), JsonConvert.SerializeObject(FgoHelpers.ServantDict, Formatting.Indented));
                        await cea.Channel.SendWithRetry($"Added alias `{alias}` for `{servant}`.");
                    }
                    catch (ArgumentException)
                    {
                        await cea.Channel.SendWithRetry($"Alias `{alias}` already exists for Servant `{FgoHelpers.ServantDict[alias]}`.");
                        return;
                    }
                });

            Console.WriteLine("Registering 'Curve'...");
            manager.Client.GetService<CommandService>().CreateCommand("curve")
                .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(_config["FGO_server"]))
                .Hide()
                .Do(async cea =>
                    await cea.Channel.SendWithRetry(
                        String.Concat(
                            "From master KyteM: `Linear curves scale as you'd expect.\n",
                            "Reverse S means their stats will grow fast, slow the fuck down as they reach the midpoint (with zero or near-zero improvements at that midpoint), then return to their previous growth speed.\n",
                            "S means the opposite. These guys get super little stats at the beginning and end, but are quite fast in the middle (Gonna guesstimate... 35 - 55 in the case of a 5 *).\n",
                            "Semi(reverse) S is like (reverse)S, except not quite as bad in the slow periods and not quite as good in the fast periods.If you graph it it'll go right between linear and non-semi.`")));

            Console.WriteLine("Registering 'Trait'...");
            manager.Client.GetService<CommandService>().CreateCommand("trait")
                .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(_config["FGO_server"]))
                .Description("Find Servants by trait.")
                .Parameter("trait", ParameterType.Required)
                .Do(async cea =>
                {
                    string trait = null;
                    var servants = FgoHelpers.ServantProfiles
                        .SelectMany(p => p.Traits.Where(t =>
                        {
                            var r = t.ContainsIgnoreCase(cea.Args[0]);
                            if (r)
                            {
                                trait = t;
                            }
                            return r;
                        })
                        .Select(s => p.Name))
                        .ToList();
                    if (trait == null)
                    {
                        await cea.Channel.SendWithRetry("Could not find trait.");
                    }
                    else if (servants.Count == 0)
                    {
                        await cea.Channel.SendWithRetry("No results for that query.");
                    }
                    else
                    {
                        await cea.Channel.SendWithRetry($"**{trait}:** {String.Join(", ", servants)}.");
                    }
                });
        }

        private static string FormatServantProfile(ServantProfile profile)
        {
            string aoe = ((profile.NoblePhantasmEffect?.Contains("AoE") == true) && Regex.Match(profile.NoblePhantasmEffect, "([0-9]+)H").Success) ? " (Hits is per enemy)" : String.Empty;
            int a = 1;
            int p = 1;
            return new StringBuilder()
                .AppendWhen(() => profile.Id < 0, b => b.AppendLine("***Not** a real profile.*"))
                .AppendWhen(() => profile.Id == -3, b => b.Append("~~"))
                .AppendLine($"**Collection ID:** {profile.Id}")
                .AppendLine($"**Rarity:** {profile.Rarity}☆")
                .AppendLine($"**Class:** {profile.Class}")
                .AppendLine($"**Servant:** {profile.Name}")
                .AppendLine($"**Gender:** {profile.Gender}")
                .AppendLine($"**Card pool:** {profile.CardPool} ({profile.B}/{profile.A}/{profile.Q}/{profile.EX}) (Fourth number is EX attack)")
                .AppendLine($"**Max ATK:** {profile.Atk}")
                .AppendLine($"**Max HP:** {profile.HP}")
                .AppendLine($"**Starweight:** {profile.Starweight}")
                .AppendLine($"**Growth type:** {profile.GrowthCurve} (Use `.curve` for explanation)")
                .AppendLine($"**NP:** {profile.NoblePhantasm} - *{profile.NoblePhantasmEffect}*{aoe}")
                .AppendWhen(() => !String.IsNullOrWhiteSpace(profile.NoblePhantasmRankUpEffect),
                    b => b.AppendLine($"**NP Rank+:** *{profile.NoblePhantasmRankUpEffect}*{aoe}"))
                .AppendLine($"**Attribute:** {profile.Attribute}")
                .AppendLine($"**Traits:** {String.Join(", ", profile.Traits)}")
                .AppendSequence(profile.ActiveSkills,
                    (b, s) =>
                    {
                        var t = b.AppendWhen(() => !String.IsNullOrWhiteSpace(s.SkillName),
                            bu => bu.AppendLine($"**Skill {a}:** {s.SkillName} {s.Rank} - *{s.Effect}*")
                                .AppendWhen(() => !String.IsNullOrWhiteSpace(s.RankUpEffect),
                                    stb => stb.AppendLine($"**Skill {a} Rank+:** *{s.RankUpEffect}*")));
                        a++;
                        return t;
                    })
                .AppendSequence(profile.PassiveSkills,
                    (b, s) =>
                    {
                        var t = b.AppendWhen(() => !String.IsNullOrWhiteSpace(s.SkillName),
                            bu => bu.AppendLine($"**Passive Skill {p}:** {s.SkillName} {s.Rank} - *{s.Effect}*"));
                        p++;
                        return t;
                    })
                    .AppendWhen(() => !String.IsNullOrWhiteSpace(profile.Additional),
                        b => b.AppendLine($"**Additional info:** {profile.Additional}"))
                    .Append(profile.Image)
                    .AppendWhen(() => profile.Id == -3, b => b.Append("~~"))
                    .ToString();
        }

    }
}
