using System;

namespace MechHisui.HisuiBets
{
    public interface IBet
    {
        string UserName  { get; }
        ulong UserId     { get; }
        string Target    { get; }
        int BettedAmount { get; }
        int BetGameId    { get; }
    }

    internal sealed class Bet : IBet
    {
        public string UserName  { get; set; }
        public ulong UserId     { get; set; }
        public string Target    { get; set; }
        public int BettedAmount { get; set; }
        public int BetGameId    { get; set; }
    }
}
