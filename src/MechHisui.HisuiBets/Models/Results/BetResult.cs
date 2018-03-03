using System;
using System.Collections.Generic;

namespace MechHisui.HisuiBets
{
    public struct BetResult
    {
        public uint RoundingLoss { get; }
        public IReadOnlyDictionary<ulong, uint> Winners { get; }

        public BetResult(uint roundingLoss, IReadOnlyDictionary<ulong, uint> winners)
        {
            RoundingLoss = roundingLoss;
            Winners = winners;
        }
    }
}
