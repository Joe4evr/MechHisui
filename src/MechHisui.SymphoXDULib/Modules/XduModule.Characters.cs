﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.SimplePermissions;
using Discord.Commands;

namespace MechHisui.SymphoXDULib
{
    public partial class XduModule
    {
        [Group("gear"), Permission(MinimumPermission.Everyone)]
        public class XduCharacters : ModuleBase<SocketCommandContext>
        {
            private readonly XduStatService _stats;

            public XduCharacters(XduStatService stats)
            {
                _stats = stats;
            }

            [Command, Alias("stats", "stat")]
            public Task CharaCmd(int id)
            {
                var profile = _stats.Config.GetGear(id);
                return (profile != null)
                    ? SendResults(profile.ToEmbedPages(), _stats, Context, listenForSelect: false)
                    : ReplyAsync("Unknown/Not a Gear ID");
            }

            [Command("search")]
            public Task SearchChara(string name)
            {
                var pages = _stats.Config.FindGears(name)
                    .SelectMany(p => p.ToEmbedPages())
                    .ToList();
                return SendResults(pages, _stats, Context, listenForSelect: true);
            }
        }
    }
}
