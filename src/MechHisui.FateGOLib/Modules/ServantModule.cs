using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.SimplePermissions;
using Discord.Commands;
using SharedExtensions;

namespace MechHisui.FateGOLib
{
    [Name("FGO")]
    public sealed partial class FgoModule : ModuleBase
    {
        [Name("Servants")]
        public sealed class ServantModule : ModuleBase<ICommandContext>
        {
            private readonly FgoStatService _service;

            public ServantModule(FgoStatService service)
            {
                _service = service;
            }

            [Command("servant"), Permission(MinimumPermission.Everyone)]
            [Alias("stat", "stats"), Priority(5)]
            [Summary("Relay information on the specified Servant by ID.")]
            public async Task StatCmd(int id)
            {
                var profile = _service.Config.GetServant(id);

                if (profile != null)
                {
                    await ReplyAsync("", embed: FormatServantProfile(profile)).ConfigureAwait(false);
                }
            }

            [Command("servant"), Permission(MinimumPermission.Everyone)]
            [Alias("stat", "stats")]
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

            //[Command("addalias"), Permission(MinimumPermission.ModRole)]
            //public Task AliasCmd(string servant, string alias)
            //{
            //    if (!_service.Config.GetServants().Any(p => p.Name == servant))
            //    {
            //        return ReplyAsync("Could not find name to add alias for.");
            //    }

            //    if (_service.Config.AddServantAlias(servant, alias.ToLowerInvariant()))
            //    {
            //        return ReplyAsync($"Added alias `{alias}` for `{servant}`.");
            //    }
            //    else
            //    {
            //        return ReplyAsync($"Alias `{alias}` already exists for Servant `{_service.Config.GetServants().Single(s => s.Aliases.Contains(alias)).Name}`.");
            //    }
            //}

            [Command("curve"), Permission(MinimumPermission.Everyone)]
            public Task CurveCmd()
             => ReplyAsync(String.Concat(
                    "From master KyteM: `Linear curves scale as you'd expect.\n",
                    "Reverse S means their stats will grow fast, slow the fuck down as they reach the midpoint (with zero or near-zero improvements at that midpoint), then return to their previous growth speed.\n",
                    "S means the opposite. These guys get super little stats at the beginning and end, but are quite fast in the middle (Gonna guesstimate... 35 - 55 in the case of a 5 *).\n",
                    "Semi(reverse) S is like (reverse)S, except not quite as bad in the slow periods and not quite as good in the fast periods.If you graph it it'll go right between linear and non-semi.`"));

            //[Command("trait"), Permission(MinimumPermission.Everyone)]
            //[Summary("Find Servants by trait.")]
            //public Task TraitCmd(string trait)
            //{
            //    string ts = null;
            //    var servants = _service.Config.GetServants()
            //        .SelectMany(p => p.Traits.Where(t =>
            //        {
            //            var r = t.ContainsIgnoreCase(trait);
            //            if (r)
            //            {
            //                ts = trait;
            //            }
            //            return r;
            //        })
            //        .Select(s => p.Name))
            //        .ToList();

            //    if (ts == null)
            //    {
            //        return ReplyAsync("Could not find trait.");
            //    }
            //    else if (servants.Count == 0)
            //    {
            //        return ReplyAsync("No results for that query.");
            //    }
            //    else
            //    {
            //        return ReplyAsync($"**{trait}:** {String.Join(", ", servants)}.");
            //    }
            //}

            private static Embed FormatServantProfile(IServantProfile profile)
            {
                var embed = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = $"Servant #{profile.Id}: {profile.Rarity}☆ {profile.Class}",
                    },
                    Title = profile.Name,
                    Fields =
                    {
                        new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = "Gender",
                            Value = profile.Gender
                        },
                        new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = "Card Pool",
                            Value = $"{profile.CardPool} ({profile.B}/{profile.A}/{profile.Q}/{profile.EX})"
                        },
                        new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = "Max ATK",
                            Value = profile.Atk
                        },
                        new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = "Max HP",
                            Value = profile.HP
                        },
                        new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = "Starweight",
                            Value = profile.Starweight
                        },
                        new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = "Growth type",
                            Value = profile.GrowthCurve
                        },
                        new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = "Attribute",
                            Value = profile.Attribute
                        },
                        new EmbedFieldBuilder
                        {
                            IsInline = false,
                            Name = "Traits",
                            Value = String.Join(", ", profile.Traits)
                        },
                        new EmbedFieldBuilder
                        {
                            IsInline = false,
                            Name = $"Noble Phantasm: ({profile.NPType}) {profile.NoblePhantasm}",
                            Value = profile.NoblePhantasmEffect
                        }
                    }
                }.WithDescriptionWhen(() => profile.Id > 0,
                    $"More information at: [Cirno]({_cirnoBaseUrl}{profile.Id}) | [FGO Wiki]({_fgoWBaseUrl}{Uri.EscapeDataString((profile.Additional ?? profile.Name).Replace(' ', '_'))})")

                .AddFieldWhen(() => !String.IsNullOrWhiteSpace(profile.NoblePhantasmRankUpEffect),
                    field => field.WithIsInline(false)
                        .WithName("NP Rank up:")
                        .WithValue(profile.NoblePhantasmRankUpEffect))

                .AddFieldSequence(profile.ActiveSkills,
                    (field, skill) => field.WithIsInline(true)
                        .WithName($"{skill.SkillName} {skill.Rank}")
                        .WithValue($"{skill.Effect}{(!String.IsNullOrWhiteSpace(skill.RankUpEffect) ? $"\n**Rank Up:** {skill.RankUpEffect}" : "")}"))

                .AddFieldSequence(profile.PassiveSkills,
                    (field, skill) => field.WithIsInline(true)
                        .WithName($"{skill.SkillName} {skill.Rank}")
                        .WithValue($"{skill.Effect}"))

                .AddFieldWhen(() => profile.Aliases.Any(),
                    field => field.WithIsInline(false)
                        .WithName("Also known as:")
                        .WithValue(String.Join(", ", profile.Aliases.Select(a => a.Alias))))

                .WithImageWhen(() => !String.IsNullOrWhiteSpace(profile.Image), profile.Image)
                .Build();

#if DEBUG

                int fields = embed.Fields.Sum(f => f.Name.Length + f.Value.Length);
                var dbg = Sum((embed.Author?.Name.Length ?? 0),
                    (embed.Title?.Length ?? 0),
                    (embed.Description?.Length ?? 0),
                    fields,
                    (embed.Footer?.Text.Length ?? 0),
                    (embed.Url?.ToString().Length ?? 0));
#endif
                return embed;
            }

            private const string _cirnoBaseUrl = "http://fate-go.cirnopedia.org/servant_profile.php?servant=";
            private const string _fgoWBaseUrl = "http://fategrandorder.wikia.com/wiki/";

#if DEBUG
            private static int Sum(params int[] ns) => ns.Sum();
#endif
        }
    }
}
