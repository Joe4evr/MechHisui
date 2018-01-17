using System;

namespace MechHisui.HisuiBets
{
    public sealed class Bet
    {
        public string UserName { get; internal set; }
        public ulong UserId { get; internal set; }
        public string Tribute { get; internal set; }
        public uint BettedAmount { get; internal set; }
    }
}
