using System;

namespace MechHisui.HisuiBets
{
    public interface IBet
    {
        ulong ChannelId   { get; }
        ulong UserId      { get; }
        string UserName   { get; }
        GameType GameType { get; }
        string Target     { get; }
        uint BettedAmount { get; }
    }

    internal sealed class Bet : IBet
    {
        public string UserName   { get; set; }
        public ulong UserId      { get; set; }
        public ulong ChannelId   { get; set; }
        public GameType GameType { get; set; }
        public string Target     { get; set; }
        public uint BettedAmount { get; set; }
    }
}
