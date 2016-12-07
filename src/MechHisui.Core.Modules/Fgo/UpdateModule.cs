using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.SimplePermissions;
using Discord.Commands;

namespace MechHisui.FateGOLib.Modules
{
    public sealed class UpdateModule : ModuleBase
    {
        private readonly StatService _statService;

        public UpdateModule(StatService statService)
        {
            _statService = statService;
        }

        [Command("update"), Permission(MinimumPermission.ModRole)]
        public async Task Update(string key)
        {
            if (_statService.UpdateFuncs.ContainsKey(key))
            {
                using (Context.Channel.EnterTypingState())
                {
                    await _statService.UpdateFuncs[key]();
                    await ReplyAsync("Updated lookup(s)");
                }
            }
        }
    }
}
