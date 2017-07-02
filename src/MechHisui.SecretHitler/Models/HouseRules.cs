using System;

namespace MechHisui.SecretHitler.Models
{
    [Flags]
    internal enum HouseRules
    {
        None = 0b0,
        SkipFirstElection = 0b1,
    }
}
