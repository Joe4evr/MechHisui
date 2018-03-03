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
    public partial class FgoModule : ModuleBase
    {
        [Name("CEs")]
        public sealed class CEModule : ModuleBase<ICommandContext>
        {
            private readonly FgoStatService _service;

            public CEModule(FgoStatService service)
            {
                _service = service;
            }

            [Command("ce"), Permission(MinimumPermission.Everyone), Priority(5)]
            public async Task CECmd(int id)
            {
                var ce = _service.Config.GetCE(id);

                if (ce != null)
                {
                    await ReplyAsync(String.Empty, embed: FormatCEProfile(ce)).ConfigureAwait(false);
                }
                else
                {
                    await CECmd(id.ToString()).ConfigureAwait(false);
                }
            }

            [Command("ce"), Permission(MinimumPermission.Everyone)]
            public async Task CECmd([Remainder] string name)
            {
                var potentials = _service.Config.FindCEs(name);
                if (potentials.Count() == 1)
                {
                    await ReplyAsync(String.Empty, embed: FormatCEProfile(potentials.Single())).ConfigureAwait(false);
                }
                else if (potentials.Count() > 1)
                {
                    var sb = new StringBuilder("Entry ambiguous. Did you mean one of the following?\n")
                        .AppendSequence(potentials, (s, pr) => s.AppendLine($"**{pr.Name}** *({String.Join(", ", pr.Name)})*"));

                    await ReplyAsync(sb.ToString()).ConfigureAwait(false);
                }
                else
                {
                    await ReplyAsync("No such entry found. Please try another name.").ConfigureAwait(false);
                }
            }

            [Command("allce"), Permission(MinimumPermission.Everyone)]
            public async Task AllCECmd([Remainder] string ceeffect)
            {
                var ces = (ceeffect == "event")
                    ? _service.Config.AllCEs().Where(c => !String.IsNullOrWhiteSpace(c.EventEffect)).ToList()
                    : _service.Config.AllCEs().Where(c => c.Effect.ContainsIgnoreCase(ceeffect)).ToList();

                if (ces.Count > 0)
                {
                    var sb = new StringBuilder($"**{ceeffect}:**\n", 2000);
                    foreach (var c in ces)
                    {
                        sb.Append($"**{c.Name}** - {c.Effect}")
                            .AppendLine(!String.IsNullOrWhiteSpace(c.EventEffect) ? $" **Event:** {c.EventEffect}" : String.Empty);
                        if (sb.Length > 1700)
                        {
                            await ReplyAsync(sb.ToString()).ConfigureAwait(false);
                            sb.Clear();
                        }
                    }
                    await ReplyAsync(sb.ToString()).ConfigureAwait(false);
                }
                else
                {
                    await ReplyAsync("No such CEs found. Please try another term.").ConfigureAwait(false);
                }
            }

            //[Command("cealias"), Permission(MinimumPermission.ModRole)]
            //public Task CEAlisCmd(string ce, string alias)
            //{
            //    if (!_service.Config.FindCEs(ce).Select(c => c.Name).Contains(ce))
            //    {
            //        return ReplyAsync("Could not find name to add alias for.");
            //    }

            //    if (_service.Config.AddCEAlias(ce, alias.ToLowerInvariant()))
            //    {
            //        return ReplyAsync($"Added alias `{alias}` for `{ce}`.");
            //    }
            //    else
            //    {
            //        return ReplyAsync($"Alias `{alias}` already exists for CE `{_service.Config.AllCEs().Single(c => c.Aliases.Any(a => a.Alias == alias)).Name}`.");
            //    }
            //}

            private static Embed FormatCEProfile(ICEProfile ce)
            {
                return new EmbedBuilder()
                    .WithAuthor(auth => auth.WithName($"CE #{ce.Id}: {ce.Rarity}☆ Cost {ce.Cost}"))
                    .WithTitle(ce.Name)
                    .AddField(field => field.WithIsInline(false)
                        .WithName("Effect / Max Effect")
                        .WithValue($"{ce.Effect} / {ce.EffectMax}"))
                    .AddFieldWhen(() => !String.IsNullOrWhiteSpace(ce.EventEffect), field => field.WithIsInline(true)
                        .WithName("Event")
                        .WithValue($"{ce.EventEffect} / {ce.EventEffectMax}"))
                    .AddField(field => field.WithIsInline(true)
                        .WithName("HP / Max HP")
                        .WithValue($"{ce.HP} / {ce.HPMax}"))
                    .AddField(field => field.WithIsInline(true)
                        .WithName("Atk / Max Atk")
                        .WithValue($"{ce.Atk} / {ce.AtkMax}"))
                    .AddFieldWhen(() => ce.Aliases.Any(),
                        field => field.WithIsInline(false)
                            .WithName("Also known as:")
                            .WithValue(String.Join(", ", ce.Aliases)))
                    .WithImageWhen(() => !String.IsNullOrWhiteSpace(ce.Image), ce.Image)
                    .Build();
            }
        }
    }
}
