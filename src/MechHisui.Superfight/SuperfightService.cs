using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Addons.MpGame;
using MechHisui.Superfight.Models;

namespace MechHisui.Superfight
{
    public sealed class SuperfightService : MpGameService<SuperfightGame, SuperfightPlayer>
    {
        private readonly ConcurrentDictionary<IMessageChannel, int> _discussionTimers
            = new ConcurrentDictionary<IMessageChannel, int>(MessageChannelComparer);

        internal ISuperfightConfig Config { get; }
        internal IReadOnlyDictionary<IMessageChannel, int> DiscussionTimers => _discussionTimers;

        public SuperfightService(
            BaseSocketClient client, ISuperfightConfig sfconfig,
            IMpGameServiceConfig? mpconfig = null, Func<LogMessage, Task>? logger = null)
            : base(client, mpconfig, logger)
        {
            Config = sfconfig ?? throw new ArgumentNullException(nameof(sfconfig));
        }

        internal void SetDiscussionTimer(IMessageChannel channel, int minutes)
            => _discussionTimers[channel] = minutes;
    }
}
