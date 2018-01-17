using System;
using System.Collections.Generic;

namespace MechHisui.HisuiBets
{
    public sealed class BetResult
    {
        public uint RoundingLoss { get; set; }
        public Dictionary<ulong, uint> Winners { get; set; }
    }
}
