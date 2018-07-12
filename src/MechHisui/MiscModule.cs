using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.SimplePermissions;

namespace MechHisui
{
    [Permission(MinimumPermission.BotOwner)]
    public sealed class MiscModule : ModuleBase<SocketCommandContext>
    {
        [Command("allroles")]
        public async Task AllGuildsRoles()
        {
            foreach (var s in Format(Context.Client.Guilds))
            {
                await ReplyAsync(s);
            }

            IEnumerable<string> Format(IEnumerable<SocketGuild> guilds)
            {
                var sb = new StringBuilder(capacity: 2000);
                foreach (var guild in guilds)
                {
                    ulong evid = guild.EveryoneRole.Id;
                    sb.AppendLine($"Roles on '{guild.Name}': ```");

                    foreach (var role in guild.Roles)
                    {
                        if (role.Id != evid)
                            sb.AppendLine($"{role.Name} : {role.Id}");

                        if (sb.Length > 1900)
                        {
                            yield return sb.Append("```").ToString();
                            sb.Clear().AppendLine($"('{guild.Name}' cont'd): ```");
                        }
                    }

                    yield return sb.Append("```").ToString();
                    sb.Clear();
                }
            }
        }
    }
}
