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
            BaseSocketClient client,
            ISuperfightConfig sfconfig,
            IMpGameServiceConfig mpconfig = null,
            Func<LogMessage, Task> logger = null)
            : base(client, mpconfig, logger)
        {
            Config = sfconfig ?? throw new ArgumentNullException(nameof(sfconfig));
        }
    }
}
