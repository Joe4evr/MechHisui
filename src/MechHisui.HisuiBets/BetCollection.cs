using System.Collections.Generic;
using System.Collections.Immutable;

namespace MechHisui.HisuiBets
{
    public sealed class BetCollection
    {
        public BetCollection(IEnumerable<Bet> bets)
        {
            Bets = bets.ToImmutableArray();
        }

        public IReadOnlyCollection<Bet> Bets { get; }
        public int Bonus { get; internal set; } = 0;
    }
}
