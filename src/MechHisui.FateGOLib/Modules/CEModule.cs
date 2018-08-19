//using System;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Discord;
//using Discord.Addons.SimplePermissions;
//using Discord.Commands;
//using SharedExtensions;

//namespace MechHisui.FateGOLib
//{
//    public partial class FgoModule
//    {
//        [Name("CEs"), Group("ce"), Alias("ces")]
//        public sealed class CEModule : FgoModule
//        {
//            private readonly FgoStatService _service;

//            public CEModule(FgoStatService service)
//            {
//                _service = service;
//            }

//            [Command, Priority(5)]
//            public async Task CECmd(int id)
//            {
//                var ce = await _service.Config.GetCEAsync(id).ConfigureAwait(false);

//                if (ce != null)
//                {
//                    await ReplyAsync(String.Empty, embed: FormatCEProfile(ce)).ConfigureAwait(false);
//                }
//                else
//                {
//                    await CECmd(id.ToString()).ConfigureAwait(false);
//                }
//            }

//            [Command]
//            public async Task CECmd([Remainder] string name)
//            {
//                var potentials = await _service.Config.FindCEsAsync(name).ConfigureAwait(false);
//                if (potentials.Count() == 1)
//                {
//                    await ReplyAsync(String.Empty, embed: FormatCEProfile(potentials.Single())).ConfigureAwait(false);
//                }
//                else if (potentials.Count() > 1)
//                {
//                    var sb = new StringBuilder("Entry ambiguous. Did you mean one of the following?\n")
//                        .AppendSequence(potentials, (s, pr) => s.AppendLine($"**{pr.Name}** *({String.Join(", ", pr.Name)})*"));

//                    await ReplyAsync(sb.ToString()).ConfigureAwait(false);
//                }
//                else
//                {
//                    await ReplyAsync("No such entry found. Please try another name.").ConfigureAwait(false);
//                }
//            }

//            [Command("effect")]
//            public async Task AllCECmd([Remainder] string ceeffect)
//            {
//                var ces = await _service.Config.FindCEsByEffectAsync(ceeffect).ConfigureAwait(false);

//                if (ces.Any())
//                {
//                    var sb = new StringBuilder($"**{ceeffect}:**\n", 2000);
//                    foreach (var c in ces)
//                    {
//                        sb.Append($"**{c.Name}** - {c.Effect}")
//                            .AppendLine(!String.IsNullOrWhiteSpace(c.EventEffect) ? $" **Event:** {c.EventEffect}" : String.Empty);
//                        if (sb.Length > 1700)
//                        {
//                            await ReplyAsync(sb.ToString()).ConfigureAwait(false);
//                            sb.Clear();
//                        }
//                    }
//                    await ReplyAsync(sb.ToString()).ConfigureAwait(false);
//                }
//                else
//                {
//                    await ReplyAsync("No such CEs found. Please try another term.").ConfigureAwait(false);
//                }
//            }

//            //[Command("cealias"), Permission(MinimumPermission.ModRole)]
//            //public Task CEAlisCmd(string ce, string alias)
//            //{
//            //    if (!_service.Config.FindCEs(ce).Select(c => c.Name).Contains(ce))
//            //    {
//            //        return ReplyAsync("Could not find name to add alias for.");
//            //    }

//            //    if (_service.Config.AddCEAlias(ce, alias.ToLowerInvariant()))
//            //    {
//            //        return ReplyAsync($"Added alias `{alias}` for `{ce}`.");
//            //    }
//            //    else
//            //    {
//            //        return ReplyAsync($"Alias `{alias}` already exists for CE `{_service.Config.AllCEs().Single(c => c.Aliases.Any(a => a.Alias == alias)).Name}`.");
//            //    }
//            //}

//            private static Embed FormatCEProfile(ICEProfile ce)
//            {
//                return new EmbedBuilder()
//                    .WithAuthor(auth => auth.WithName($"CE #{ce.Id}: {ce.Rarity}☆ Cost {ce.Cost}"))
//                    .WithTitle(ce.Name)
//                    .AddField(field => field.WithIsInline(false)
//                        .WithName("Effect / Max Effect")
//                        .WithValue($"{ce.Effect} / {ce.EffectMax}"))
//                    .AddFieldWhen(() => !String.IsNullOrWhiteSpace(ce.EventEffect), field => field.WithIsInline(true)
//                        .WithName("Event")
//                        .WithValue($"{ce.EventEffect} / {ce.EventEffectMax}"))
//                    .AddField(field => field.WithIsInline(true)
//                        .WithName("HP / Max HP")
//                        .WithValue($"{ce.HP} / {ce.HPMax}"))
//                    .AddField(field => field.WithIsInline(true)
//                        .WithName("Atk / Max Atk")
//                        .WithValue($"{ce.Atk} / {ce.AtkMax}"))
//                    .AddFieldWhen(() => ce.Aliases.Any(),
//                        field => field.WithIsInline(false)
//                            .WithName("Also known as:")
//                            .WithValue(String.Join(", ", ce.Aliases)))
//                    .WithImageIfNotNull(ce.Image)
//                    .Build();
//            }
//        }
//    }
//}
