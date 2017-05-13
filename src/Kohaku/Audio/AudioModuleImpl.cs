using System;
using Discord.Commands;
using Discord.Addons.SimpleAudio;
using Discord;
using System.Threading.Tasks;

namespace Kohaku
{
    public class AudioModuleImpl : AudioModule
    {
        public AudioModuleImpl(AudioService service) : base(service)
        {
        }

#pragma warning disable RCS1132 // Remove redundant overriding member.
        [Command("join", RunMode = RunMode.Async)]
        public override Task JoinCmd(IVoiceChannel target = null)
        {
            return base.JoinCmd(target);
        }

        [Command("list", RunMode = RunMode.Async)]
        public override Task ListCmd()
        {
            return base.ListCmd();
        }

        [Command("play", RunMode = RunMode.Async)]
        public override Task PlayCmd([Remainder] string song)
        {
            return base.PlayCmd(song);
        }

        [Command("next", RunMode = RunMode.Async)]
        public override Task NextCmd()
        {
            return base.NextCmd();
        }

        [Command("stop", RunMode = RunMode.Async)]
        public override Task StopCmd()
        {
            return base.StopCmd();
        }

        [Command("leave", RunMode = RunMode.Async)]
        public override Task LeaveCmd()
        {
            return base.LeaveCmd();
        }
#pragma warning restore RCS1132 // Remove redundant overriding member.
    }
}