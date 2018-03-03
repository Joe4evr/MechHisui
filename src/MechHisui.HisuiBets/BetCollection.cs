using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MechHisui.HisuiBets
{
    public sealed class BetCollection
    {
        public BetCollection(IEnumerable<IBet> bets)
        {
            Bets = bets.ToImmutableArray();
        }

        public IReadOnlyCollection<IBet> Bets { get; }
        public int Bonus { get; internal set; } = 0;
        public uint WholeSum => (uint)(Bets.Sum(b => b.BettedAmount) + Bonus);
    }
}
