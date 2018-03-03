using System;

namespace MechHisui.HisuiBets
{
    public enum RecordingResult
    {
        MiscError            = 0,
        BetAdded             = 1,
        BetReplaced          = 2,
        CannotReplaceOldBet  = 3,
        NewBetLessThanOldBet = 4
    }
}
