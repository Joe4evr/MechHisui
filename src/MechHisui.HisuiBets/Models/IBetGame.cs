using System;
using System.Collections.Generic;

namespace MechHisui.HisuiBets
{
    public interface IBetGame
    {
        int Id                 { get; }
        ulong ChannelId        { get; }
        bool IsCollected       { get; }
        bool IsCashedOut       { get; }
        GameType GameType      { get; }
        IEnumerable<IBet> Bets { get; }
    }
}
