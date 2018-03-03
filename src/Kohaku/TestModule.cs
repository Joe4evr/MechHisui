using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Addons.SimplePermissions;

namespace Kohaku
{
    [Group("test"), Name("Test")]
    public class TestModule : ModuleBase<SocketCommandContext>
    {
        [Command("echo"), Permission(MinimumPermission.Everyone)]
        public Task RegisterFC([Remainder] string text)
        {
            return ReplyAsync($"**Echo:** {text}");
        }
    }
}
