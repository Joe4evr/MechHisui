using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using JiiLib;
using Discord;
using Discord.Commands;
using Discord.Addons.SimplePermissions;

namespace MechHisui.FateGOLib.Modules
{
    public sealed class ServantStatsModule : ModuleBase
    {
        private readonly StatService _statService;
        private readonly string[] neverever = new[] { "enkidu", "arc", "arcueid" };

        public ServantStatsModule(StatService statService)
        {
            _statService = statService;
        }

        [Command("servant"), Permission(MinimumPermission.Everyone)]
        [Alias("stat", "stats")]
        public async Task ServantCmd(string name)
        {
            if (name.Equals("waifu", StringComparison.OrdinalIgnoreCase))
            {
                await ReplyAsync("It has come to my attention that your 'waifu' is equatable to fecal matter.");
                return;
            }

            if (neverever.ContainsIgnoreCase(name))
            {
                await ReplyAsync("Never ever.");
                return;
            }

            var potentials = _statService.LookupStats(name);
            if (potentials.Count() == 1)
            {
                await ReplyAsync(FormatServantProfile(potentials.Single()));
            }
            else if (potentials.Count() > 1)
            {
                var sb = new StringBuilder("Entry ambiguous. Did you mean one of the following?\n")
                    .AppendSequence(potentials, (s, pr) => s.AppendLine($"**{pr.Name}** *({String.Join(", ", FgoHelpers.ServantDict.Where(d => d.Value == pr.Name).Select(d => d.Key))})*"));

                await ReplyAsync(sb.ToString());
            }
            else
            {
                await ReplyAsync("No such entry found. Please try another name.");
            }
        }

        [Command("servant"), Permission(MinimumPermission.Everyone)]
        [Alias("stat", "stats")]
        public async Task ServantCmd(int id)
        {
            var profile = FgoHelpers.ServantProfiles.SingleOrDefault(p => p.Id == id) ??
                FgoHelpers.FakeServantProfiles.SingleOrDefault(p => p.Id == id);

            await ReplyAsync(FormatServantProfile(profile));
        }

        [Command("addalias"), Permission(MinimumPermission.ModRole)]
        public async Task ServantAliasCmd(string servant, string alias)
        {
            if (!FgoHelpers.ServantProfiles.Select(p => p.Name).Contains(servant))
            {
                await ReplyAsync("Could not find name to add alias for.");
                return;
            }

            var a = alias.ToLowerInvariant();
            if (!FgoHelpers.ServantDict.ContainsKey(a))
            {
                FgoHelpers.ServantDict.Add(a, servant);
                File.WriteAllText(_statService.Config.ServantAliasesPath, JsonConvert.SerializeObject(FgoHelpers.ServantDict, Formatting.Indented));
                await ReplyAsync($"Added alias `{a}` for `{servant}`.");
            }
            else
            {
                await ReplyAsync($"Alias `{a}` already exists for Servant `{FgoHelpers.ServantDict[alias]}`.");
                return;
            }
        }

        [Command("curve"), Permission(MinimumPermission.Everyone)]
        [Alias("curves")]
        public Task CurveCmd()
            => ReplyAsync(String.Concat(
                "From master KyteM: `Linear curves scale as you'd expect.\n",
                "Reverse S means their stats will grow fast, slow the fuck down as they reach the midpoint (with zero or near-zero improvements at that midpoint), then return to their previous growth speed.\n",
                "S means the opposite. These guys get super little stats at the beginning and end, but are quite fast in the middle (Gonna guesstimate... 35 - 55 in the case of a 5 *).\n",
                "Semi(reverse) S is like (reverse)S, except not quite as bad in the slow periods and not quite as good in the fast periods.If you graph it it'll go right between linear and non-semi.`"));

        [Command("trait"), Permission(MinimumPermission.Everyone)]
        public async Task TraitCmd(string trait)
        {
            string x = null;
            var servants = FgoHelpers.ServantProfiles
                .SelectMany(p => p.Traits.Where(t =>
                {
                    var r = t.Trait.ContainsIgnoreCase(trait);
                    if (r)
                    {
                        x = t.Trait;
                    }
                    return r;
                })
                .Select(s => p.Name))
                .ToList();
            if (x == null)
            {
                await ReplyAsync("Could not find trait.");
            }
            else if (servants.Count == 0)
            {
                await ReplyAsync("No results for that query.");
            }
            else
            {
                await ReplyAsync($"**{x}:** {String.Join(", ", servants)}.");
            }
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
