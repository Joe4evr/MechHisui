using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.SimplePermissions;
using Discord.Commands;
using Discord.WebSocket;
using MechHisui.Core;

namespace MechHisui.HisuiBets
{
    [Name("HisuiBets")]
    public sealed class HisuiBankModule : ModuleBase
    {
        private readonly MechHisuiContext _db;
        public HisuiBankModule(MechHisuiContext db)
        {
            _db = db;
        }

        [Command("mybucks"), Alias("bucks")]
        [Permission(MinimumPermission.Everyone)]
        public async Task MyBucks()
        {
            var bucks = _db.UserAccounts.Single(u => u.UserId == Context.User.Id).Bucks;
            await ReplyAsync($"**{Context.User.Username}** currently has {HisuiBankService.symbol}{bucks}.");
        }
    }
}
