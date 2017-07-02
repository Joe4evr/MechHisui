using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Addons.SimplePermissions;
using Discord.Commands;
using JiiLib;

namespace MechHisui.FateGOLib.Modules
{
    [Name("MysticCodes")]
    public class MysticModule : ModuleBase<ICommandContext>
    {
        private readonly StatService _service;

        public MysticModule(StatService service)
        {
            _service = service;
        }

        [Command("mystic"), Permission(MinimumPermission.Everyone)]
        public async Task MysticCmd(string name)
        {
            var codes = _service.LookupMystic(name);

            if (codes.Count() == 1)
            {
                await ReplyAsync(FormatMysticCodeProfile(codes.Single())).ConfigureAwait(false);
            }
            else if (codes.Count() > 1)
            {
                var sb = new StringBuilder("Entry ambiguous. Did you mean one of the following?\n")
                    .AppendSequence(codes, (s, m) => s.AppendLine($"**{m.Code}** *({String.Join(", ", m.Aliases)})*"));

                await ReplyAsync(sb.ToString()).ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync("Specified Mystic Code not found. Please use `.listmystic` for the list of available Mystic Codes.").ConfigureAwait(false);
            }
        }

        [Command("listmystic"), Permission(MinimumPermission.Everyone)]
        public Task ListMysticsCmd()
        {
            return ReplyAsync(String.Join("\n", _service.Config.GetMystics().Select(m => $"**{m.Code}** *({String.Join(", ", m.Aliases)})*")));
        }

        [Command("mysticalias"), Permission(MinimumPermission.ModRole)]
        public Task MysticAliasCmd(string code, string alias)
        {
            if (!_service.Config.GetMystics().Select(c => c.Code).Contains(code))
            {
                return ReplyAsync("Could not find name to add alias for.");
            }

            if (_service.Config.AddMysticAlias(code, alias.ToLowerInvariant()))
            {
                return ReplyAsync($"Added alias `{alias}` for `{code}`.");
            }
            else
            {
                return ReplyAsync($"Alias `{alias}` already exists for CE `{_service.Config.GetMystics().Single(c => c.Aliases.Contains(alias)).Code}`.");
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
