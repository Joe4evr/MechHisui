using System;
using System.Collections.Generic;

namespace MechHisui.HisuiBets
{
    public struct BetResult
    {
        public int RoundingLoss { get; }
        public IReadOnlyDictionary<ulong, int> Winners { get; }

        public BetResult(int roundingLoss, IReadOnlyDictionary<ulong, int> winners)
        {
            RoundingLoss = roundingLoss;
            Winners = winners;
        }
    }
}
