#if !ARM
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Addons.SimpleAudio;
using Discord.Addons.SimplePermissions;

namespace DivaBot
{
    [Name("Jukebox")]
    public sealed class AudioModImpl : AudioModule
    {
        public AudioModImpl(AudioService service) : base(service)
        {
        }

#pragma warning disable RCS1132 // Remove redundant overriding member.
        [Command("join", RunMode = RunMode.Async)]
        [Permission(MinimumPermission.Everyone)]
        public override Task JoinCmd(IVoiceChannel target = null)
        {
            return base.JoinCmd(target);
        }

        [Command("listsongs", RunMode = RunMode.Async)]
        [Permission(MinimumPermission.Everyone)]
        public override Task ListCmd()
        {
            return base.ListCmd();
        }

        [Command("play", RunMode = RunMode.Async)]
        [Permission(MinimumPermission.Everyone)]
        public override Task PlayCmd([Remainder] string song)
        {
            return base.PlayCmd(song);
        }

        [Command("next", RunMode = RunMode.Async)]
        [Permission(MinimumPermission.Everyone)]
        public override Task NextCmd()
        {
            return base.NextCmd();
        }

        [Command("stop", RunMode = RunMode.Async)]
        [Permission(MinimumPermission.Everyone)]
        public override Task StopCmd()
        {
            return base.StopCmd();
        }

        [Command("leave", RunMode = RunMode.Async)]
        [Permission(MinimumPermission.Everyone)]
        public override Task LeaveCmd()
        {
            return base.LeaveCmd();
        }
#pragma warning restore RCS1132 // Remove redundant overriding member.
    }
}
#endif
