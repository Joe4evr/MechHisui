using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Addons.MpGame;
using MechHisui.Superfight.Models;

namespace MechHisui.Superfight
{
    public sealed class SuperfightService : MpGameService<SuperfightGame, SuperfightPlayer>
    {
        internal ISuperfightConfig Config { get; }
        internal ConcurrentDictionary<IMessageChannel, int> DiscussionTimer { get; } = new ConcurrentDictionary<IMessageChannel, int>(MessageChannelComparer);

        public SuperfightService(
            ISuperfightConfig config,
            DiscordSocketClient client,
            Func<LogMessage, Task> logger = null)
            : base(client, logger)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
        }
    }

    //public static class SFExtensions
    //{
    //    public static Task AddSuperFight(
    //        this CommandService cmds,
    //        IServiceCollection map,
    //        string cfgBasepath)
    //    {
    //        map.AddSingleton(new SuperfightService(cfgBasepath));
    //        return cmds.AddModuleAsync<SuperfightModule>();
    //    }
    //}
}
