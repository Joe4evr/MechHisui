//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Discord;
//using Discord.Commands;
//using MechHisui.FateGOLib;

//namespace Kohaku
//{
//    [Name("FgoTest")]
//    public class TestModule : ModuleBase<ICommandContext>
//    {
//        private readonly List<ServantProfile> _profiles;

//        public TestModule(List<ServantProfile> profiles)
//        {
//            _profiles = profiles;
//        }

//        [Command("stats")]
//        public Task StatsCmd(int id)
//        {
//            var embed = FormatProfile(_profiles[id - 1]);
//            return base.ReplyAsync("", embed: embed);
//        }

//    }

//    internal static class Ext
//    {
//        internal static EmbedBuilder AddFieldSequence<T>(
//                this EmbedBuilder builder,
//                IEnumerable<T> seq,
//                Action<EmbedFieldBuilder, T> action)
//        {
//            foreach (var item in seq)
//            {
//                builder.AddField(efb => action(efb, item));
//            }

//            return builder;
//        }

//        internal static EmbedBuilder AddFieldWhen(
//                this EmbedBuilder builder,
//                Func<bool> predicate,
//                Action<EmbedFieldBuilder> field)
//        {
//            if (predicate())
//            {
//                builder.AddField(field);
//            }

//            return builder;
//        }
//    }
//}
