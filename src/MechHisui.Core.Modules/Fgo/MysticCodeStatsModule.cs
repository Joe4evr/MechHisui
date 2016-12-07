using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JiiLib;
using Newtonsoft.Json;
using Discord.Commands;
using Discord.Addons.SimplePermissions;

namespace MechHisui.FateGOLib.Modules
{
    public sealed class MysticCodeStatsModule : ModuleBase
    {
        private readonly StatService _statService;

        public MysticCodeStatsModule(StatService statService)
        {
            _statService = statService;
        }

        [Command("mystic"), Permission(MinimumPermission.Everyone)]
        public async Task MysticCmd(string name)
        {
            var codes = _statService.LookupMystic(name);

            if (codes.Count() == 1)
            {
                await ReplyAsync(FormatMysticCodeProfile(codes.Single()));
            }
            else if (codes.Count() > 1)
            {
                var sb = new StringBuilder("Entry ambiguous. Did you mean one of the following?\n")
                    .AppendSequence(codes, (s, m) => s.AppendLine($"**{m.Code}** *({String.Join(", ", FgoHelpers.MysticCodeDict.Where(d => d.Value == m.Code).Select(d => d.Key))})*"));

                await ReplyAsync(sb.ToString());
            }
            else
            {
                await ReplyAsync("Specified Mystic Code not found. Please use `.listmystic` for the list of available Mystic Codes.");
            }
        }

        [Command("listmystic"), Permission(MinimumPermission.Everyone)]
        public Task ListMysticCmd()
         => ReplyAsync("**Available Mystic Codes:**\n" +
             String.Join("\n", FgoHelpers.MysticCodeList.Select(m => m.Code)));

        [Command("mysticalias"), Permission(MinimumPermission.ModRole)]
        public async Task MysticAliasCmd(string code, string alias)
        {
            if (!FgoHelpers.MysticCodeList.Select(m => m.Code).Contains(code))
            {
                await ReplyAsync("Could not find Mystic Code to add alias for.");
                return;
            }

            var a = alias.ToLowerInvariant();
            if (!FgoHelpers.MysticCodeDict.ContainsKey(a))
            {
                FgoHelpers.MysticCodeDict.Add(alias, code);
                File.WriteAllText(_statService.Config.MysticAliasesPath, JsonConvert.SerializeObject(FgoHelpers.MysticCodeDict, Formatting.Indented));
                await ReplyAsync($"Added alias `{a}` for `{code}`.");
            }
            else
            {
                await ReplyAsync($"Alias `{a}` already exists for CE `{FgoHelpers.MysticCodeDict[a]}`.");
                return;
            }
        }

        private static string FormatMysticCodeProfile(MysticCode code)
        {
            StringBuilder sb = new StringBuilder()
                .AppendLine($"**Name:** {code.Code}")
                .AppendLine($"**Skill 1:** {code.Skill1} - *{code.Skill1Effect}*")
                .AppendLine($"**Skill 2:** {code.Skill2} - *{code.Skill2Effect}*")
                .AppendLine($"**Skill 3:** {code.Skill3} - *{code.Skill3Effect}*")
                .Append(code.Image);
            return sb.ToString();
        }

    }
}
