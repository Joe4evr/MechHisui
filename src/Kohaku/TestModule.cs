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
        private readonly TestService _service;

        public TestModule(TestService service)
        {
            _service = service;
        }

        [Command("fc")]
        public Task RegisterFC([Remainder] string code)
        {
            //validate code
            _service.SetFgoCode(Context.User, code);
            return ReplyAsync("Friend code set");
        }
    }
}
