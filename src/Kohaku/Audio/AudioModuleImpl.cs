//using System;
//using System.Threading.Tasks;
//using Discord.Commands;
//using Discord.Addons.SimpleAudio;
//using Discord;

//namespace Kohaku
//{
//    public class AudioModuleImpl : AudioModule
//    {
//        public AudioModuleImpl(AudioService service) : base(service)
//        {
//        }

//#pragma warning disable RCS1132 // Remove redundant overriding member.
//        [Command("join", RunMode = RunMode.Async)]
//        public override Task JoinCmd(IVoiceChannel target = null)
//        {
//            return base.JoinCmd(target);
//        }

//        [Command("list", RunMode = RunMode.Async)]
//        public override Task ListCmd()
//        {
//            return base.ListCmd();
//        }

//        [Command("play", RunMode = RunMode.Async)]
//        public override Task PlayCmd([Remainder] string song)
//        {
//            return base.PlayCmd(song);
//        }

//        [Command("playlist", RunMode = RunMode.Async)]
//        public override Task PlaylistCmd()
//        {
//            return base.PlaylistCmd();
//        }

//        [Command("pause", RunMode = RunMode.Async)]
//        public override Task PauseCmd()
//        {
//            return base.PauseCmd();
//        }

//        [Command("resume", RunMode = RunMode.Async), Alias("unpause")]
//        public override Task ResumeCmd()
//        {
//            return base.ResumeCmd();
//        }

//        [Command("next", RunMode = RunMode.Async)]
//        public override Task NextCmd()
//        {
//            return base.NextCmd();
//        }

//        [Command("stop", RunMode = RunMode.Async)]
//        public override Task StopCmd()
//        {
//            return base.StopCmd();
//        }

//        [Command("setvol", RunMode = RunMode.Async)]
//        public override Task SetVolumeCmd([Range(1, 100)] int percentage)
//        {
//            return base.SetVolumeCmd(percentage);
//        }

//        [Command("leave", RunMode = RunMode.Async)]
//        public override Task LeaveCmd()
//        {
//            return base.LeaveCmd();
//        }
//#pragma warning restore RCS1132 // Remove redundant overriding member.
//    }
//}