﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.SimplePermissions;
using Discord.Commands;
using JiiLib;

namespace MechHisui.FateGOLib.Modules
{
    [Name("Servants")]
    public class ServantModule : ModuleBase<ICommandContext>
    {
        private readonly StatService _service;

        public ServantModule(StatService service)
        {
            _service = service;
        }

        [Command("servant"), Permission(MinimumPermission.Everyone)]
        [Alias("stat", "stats")]
        [Summary("Relay information on the specified Servant by ID.")]
        public async Task StatCmd(int id)
        {
            var profile = _service.Config.GetServants().SingleOrDefault(p => p.Id == id) ??
                _service.Config.GetFakeServants().SingleOrDefault(p => p.Id == id);

            if (profile != null)
            {
                await ReplyAsync("", embed: FormatServantProfile(profile)).ConfigureAwait(false);
            }
        }

        [Command("servant"), Permission(MinimumPermission.Everyone)]
        [Alias("stat", "stats"), Priority(5)]
        [Summary("Relay information on the specified Servant. Alternative names acceptable.")]
        public Task StatCmd([Remainder] string name)
        {
            if (name.ContainsIgnoreCase("waifu"))
            {
                return ReplyAsync("It has come to my attention that your 'waifu' is equatable to fecal matter.");
            }

            if (new[] { "arc", "arcueid" }.ContainsIgnoreCase(name))
            {
                return ReplyAsync("Never ever.");
            }

            var potentials = _service.LookupStats(name);
            if (potentials.Count() == 1)
            {
                return ReplyAsync("", embed: FormatServantProfile(potentials.Single()));
            }
            else if (potentials.Count() > 1)
            {
                //var aliases = _service.Config.GetServantAliases();
                var sb = new StringBuilder("Entry ambiguous. Did you mean one of the following?\n")
                    .AppendSequence(potentials, (s, pr) => s.AppendLine($"**{pr.Name}** *({String.Join(", ", pr.Aliases)})*"));

                return ReplyAsync(sb.ToString());
            }
            else
            {
                return ReplyAsync("No such entry found. Please try another name.");
            }
        }

        [Command("addalias"), Permission(MinimumPermission.ModRole)]
        public Task AliasCmd(string servant, string alias)
        {
            if (!_service.Config.GetServants().Any(p => p.Name == servant))
            {
                return ReplyAsync("Could not find name to add alias for.");
            }

            if (_service.Config.AddServantAlias(servant, alias.ToLowerInvariant()))
            {
                return ReplyAsync($"Added alias `{alias}` for `{servant}`.");
            }
            else
            {
                return ReplyAsync($"Alias `{alias}` already exists for Servant `{_service.Config.GetServants().Single(s => s.Aliases.Contains(alias)).Name}`.");
            }
        }

        [Command("curve"), Permission(MinimumPermission.Everyone)]
        public Task CurveCmd()
         => ReplyAsync(String.Concat(
                "From master KyteM: `Linear curves scale as you'd expect.\n",
                "Reverse S means their stats will grow fast, slow the fuck down as they reach the midpoint (with zero or near-zero improvements at that midpoint), then return to their previous growth speed.\n",
                "S means the opposite. These guys get super little stats at the beginning and end, but are quite fast in the middle (Gonna guesstimate... 35 - 55 in the case of a 5 *).\n",
                "Semi(reverse) S is like (reverse)S, except not quite as bad in the slow periods and not quite as good in the fast periods.If you graph it it'll go right between linear and non-semi.`"));

        [Command("trait"), Permission(MinimumPermission.Everyone)]
        [Summary("Find Servants by trait.")]
        public Task TraitCmd(string trait)
        {
            string ts = null;
            var servants = _service.Config.GetServants()
                .SelectMany(p => p.Traits.Where(t =>
                {
                    var r = t.ContainsIgnoreCase(trait);
                    if (r)
                    {
                        ts = trait;
                    }
                    return r;
                })
                .Select(s => p.Name))
                .ToList();

            if (ts == null)
            {
                return ReplyAsync("Could not find trait.");
            }
            else if (servants.Count == 0)
            {
                return ReplyAsync("No results for that query.");
            }
            else
            {
                return ReplyAsync($"**{trait}:** {String.Join(", ", servants)}.");
            }
        }

        private static Embed FormatServantProfile(ServantProfile profile)
        {
            return new EmbedBuilder()
                .WithAuthor(auth => auth.WithName($"Servant #{profile.Id}: {profile.Rarity}☆ {profile.Class}"))
                .WithTitle(profile.Name)
                .WithDescription($"More information at: [Cirno](http://fate-go.cirnopedia.org/servant_profile.php?servant={profile.Id})")
                .AddField(field => field.WithIsInline(true)
                    .WithName("Gender")
                    .WithValue(profile.Gender))
                .AddField(field => field.WithIsInline(true)
                    .WithName("Card pool")
                    .WithValue($"{profile.CardPool} ({profile.B}/{profile.A}/{profile.Q}/{profile.EX})"))
                .AddField(field => field.WithIsInline(true)
                    .WithName("Max ATK")
                    .WithValue(profile.Atk.ToString()))
                .AddField(field => field.WithIsInline(true)
                    .WithName("Max HP")
                    .WithValue(profile.HP.ToString()))
                .AddField(field => field.WithIsInline(true)
                    .WithName("Starweight")
                    .WithValue(profile.Starweight.ToString()))
                .AddField(field => field.WithIsInline(true)
                    .WithName("Growth type")
                    .WithValue(profile.GrowthCurve))
                .AddField(field => field.WithIsInline(true)
                    .WithName("Attribute")
                    .WithValue(profile.Attribute))
                .AddField(field => field.WithIsInline(false)
                    .WithName("Traits")
                    .WithValue(String.Join(", ", profile.Traits)))
                .AddField(field => field.WithIsInline(false)
                    .WithName($"Noble Phantasm: ({profile.NPType}) {profile.NoblePhantasm}")
                    .WithValue(profile.NoblePhantasmEffect))
                .AddFieldWhen(() => !String.IsNullOrWhiteSpace(profile.NoblePhantasmRankUpEffect),
                    field => field.WithIsInline(false)
                        .WithName("NP Rank up:")
                        .WithValue(profile.NoblePhantasmRankUpEffect))
                .AddFieldSequence(profile.ActiveSkills,
                    (field, skill) => field.WithIsInline(true)
                        .WithName($"{skill.SkillName} {skill.Rank}")
                        .WithValue($"{skill.Effect}{(!String.IsNullOrWhiteSpace(skill.RankUpEffect) ? $"\nRank Up: {skill.RankUpEffect}" : "")}"))
                .AddFieldSequence(profile.PassiveSkills,
                    (field, skill) => field.WithIsInline(true)
                        .WithName($"{skill.SkillName} {skill.Rank}")
                        .WithValue($"{skill.Effect}"))
                .AddFieldWhen(() => profile.Aliases.Any(),
                    field => field.WithIsInline(false)
                        .WithName("Also known as:")
                        .WithValue(String.Join(", ", profile.Aliases)))
                .WithImageWhen(() => !String.IsNullOrWhiteSpace(profile.Image), profile.Image)
                .Build();
        }

        //private static string FormatServantProfile(ServantProfile profile)
        //{
        //    string aoe = ((profile.NoblePhantasmEffect?.Contains("AoE") == true) && Regex.Match(profile.NoblePhantasmEffect, "([0-9]+)H").Success) ? " (Hits is per enemy)" : String.Empty;
        //    int a = 1;
        //    int p = 1;
        //    return new StringBuilder(2000)
        //        .AppendWhen(() => profile.Id < 0, b => b.AppendLine("_**Not** a real profile_"))
        //        .AppendWhen(() => profile.Id == -3, b => b.Append("~~"))
        //        .AppendLine($"**Collection ID:** {profile.Id}")
        //        .AppendLine($"**Rarity:** {profile.Rarity}☆")
        //        .AppendLine($"**Class:** {profile.Class}")
        //        .AppendLine($"**Servant:** {profile.Name}")
        //        .AppendLine($"**Gender:** {profile.Gender}")
        //        .AppendLine($"**Card pool:** {profile.CardPool} ({profile.B}/{profile.A}/{profile.Q}/{profile.EX}) (Fourth number is EX attack)")
        //        .AppendLine($"**Max ATK:** {profile.Atk}")
        //        .AppendLine($"**Max HP:** {profile.HP}")
        //        .AppendLine($"**Starweight:** {profile.Starweight}")
        //        .AppendLine($"**Growth type:** {profile.GrowthCurve} (Use `.curve` for explanation)")
        //        .AppendLine($"**NP:** ({profile.NPType}) {profile.NoblePhantasm} - *{profile.NoblePhantasmEffect}*{aoe}")
        //        .AppendWhen(() => !String.IsNullOrWhiteSpace(profile.NoblePhantasmRankUpEffect),
        //            b => b.AppendLine($"**NP Rank+:** *{profile.NoblePhantasmRankUpEffect}*{aoe}"))
        //        .AppendLine($"**Attribute:** {profile.Attribute}")
        //        .AppendLine($"**Traits:** {String.Join(", ", profile.Traits)}")
        //        .AppendSequence(profile.ActiveSkills,
        //            (b, s) =>
        //            {
        //                var t = b.AppendWhen(() => !String.IsNullOrWhiteSpace(s.SkillName),
        //                    bu => bu.AppendLine($"**Skill {a}:** {s.SkillName} {s.Rank} - *{s.Effect}*")
        //                        .AppendWhen(() => !String.IsNullOrWhiteSpace(s.RankUpEffect),
        //                            stb => stb.AppendLine($"**Skill {a} Rank+:** *{s.RankUpEffect}*")));
        //                a++;
        //                return t;
        //            })
        //        .AppendSequence(profile.PassiveSkills,
        //            (b, s) =>
        //            {
        //                var t = b.AppendWhen(() => !String.IsNullOrWhiteSpace(s.SkillName),
        //                    bu => bu.AppendLine($"**Passive Skill {p}:** {s.SkillName} {s.Rank} - *{s.Effect}*"));
        //                p++;
        //                return t;
        //            })
        //            .AppendWhen(() => !String.IsNullOrWhiteSpace(profile.Additional),
        //                b => b.AppendLine($"**Additional info:** {profile.Additional}"))
        //            .Append(profile.Image)
        //            .AppendWhen(() => profile.Id == -3, b => b.Append("~~"))
        //            .ToString();
        //}
    }
}