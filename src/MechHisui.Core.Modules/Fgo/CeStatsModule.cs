using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using JiiLib;
using Newtonsoft.Json;
using Discord.Commands;
using Discord.Addons.SimplePermissions;

namespace MechHisui.FateGOLib.Modules
{
    public sealed class CeStatsModule : ModuleBase
    {
        private readonly StatService _statService;

        public CeStatsModule(StatService statService)
        {
            _statService = statService;
        }

        [Command("ce"), Permission(MinimumPermission.Everyone)]
        public async Task CeCmd(string name)
        {
            var potentials = _statService.LookupCE(name);
            if (potentials.Count() == 1)
            {
                await ReplyAsync(FormatCEProfile(potentials.Single()));
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

        [Command("ce"), Permission(MinimumPermission.Everyone)]
        public async Task CeCmd(int id)
        {
            var ce = FgoHelpers.CEProfiles.SingleOrDefault(p => p.Id == id);
            if (ce != null)
                await ReplyAsync(FormatCEProfile(ce));
        }

        [Command("allce"), Permission(MinimumPermission.Everyone)]
        public async Task AllCeCmd([Remainder] string effect)
        {
            var ces = (effect.Equals("event", StringComparison.OrdinalIgnoreCase))
                ? FgoHelpers.CEProfiles.Where(c => !String.IsNullOrWhiteSpace(c.EventEffect)).ToList()
                : FgoHelpers.CEProfiles.Where(c => c.Effect.ContainsIgnoreCase(effect)).ToList();

            if (ces.Count() > 0)
            {
                var sb = new StringBuilder($"**{effect}:**\n", 2000);
                foreach (var c in ces)
                {
                    sb.Append($"**{c.Name}** - {c.Effect}")
                        .AppendLine(!String.IsNullOrWhiteSpace(c.EventEffect) ? $" **Event:** {c.EventEffect}" : "");
                    if (sb.Length > 1700)
                    {
                        await ReplyAsync(sb.ToString());
                        sb.Clear();
                    }
                }
                await ReplyAsync(sb.ToString());
            }
            else
            {
                await ReplyAsync("No such CEs found. Please try another term.");
            }
        }

        [Command("cealias"), Permission(MinimumPermission.ModRole)]
        public async Task CeAliasCmd(string ce, string alias)
        {
            if (!FgoHelpers.CEProfiles.Select(c => c.Name).Contains(ce))
            {
                await ReplyAsync("Could not find name to add alias for.");
                return;
            }

            var a = alias.ToLowerInvariant();
            if (!FgoHelpers.CEDict.ContainsKey(a))
            {
                FgoHelpers.CEDict.Add(a, ce);
                File.WriteAllText(_statService.Config.CEAliasesPath, JsonConvert.SerializeObject(FgoHelpers.CEDict, Formatting.Indented));
                await ReplyAsync($"Added alias `{a}` for `{ce}`.");
            }
            else
            {
                await ReplyAsync($"Alias `{a}` already exists for CE `{FgoHelpers.CEDict[a]}`.");
                return;
            }
        }

        private static string FormatCEProfile(CEProfile ce)
        {
            return new StringBuilder()
                .AppendLine($"**Collection ID:** {ce.Id}")
                .AppendLine($"**Rarity:** {ce.Rarity}☆")
                .AppendLine($"**CE:** {ce.Name}")
                .AppendLine($"**Cost:** {ce.Cost}")
                .AppendLine($"**ATK:** {ce.Atk}")
                .AppendLine($"**HP:** {ce.HP}")
                .AppendLine($"**Effect:** {ce.Effect}")
                .AppendLine($"**Max ATK:** {ce.AtkMax}")
                .AppendLine($"**Max HP:** {ce.HPMax}")
                .AppendLine($"**Max Effect:** {ce.EffectMax}")
                .Append(ce.Image)
                .ToString();
        }
    }
}
