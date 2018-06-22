using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Discord;

namespace MechHisui.HisuiBets
{
    public sealed class BetCollection
    {
        internal BetCollection(IBetGame game)
        {
            GameId    = game.Id;
            ChannelId = game.ChannelId;
            Bets      = game.Bets.ToImmutableArray();
        }

        public int GameId { get; }
        public ulong ChannelId { get; }
        public ImmutableArray<IBet> Bets { get; }
        public int WholeSum => Bets.Sum(b => b.BettedAmount) + Bonus;

        internal int Bonus { get; set; } = 0;
    }
}
