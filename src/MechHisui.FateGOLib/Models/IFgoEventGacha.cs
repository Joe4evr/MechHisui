using System;
using System.Collections.Generic;

namespace MechHisui.FateGOLib
{
    public interface IFgoEventGacha
    {
        DateTimeOffset StartTime { get; }
        DateTimeOffset EndTime { get; }
        IEnumerable<IServantProfile> RateUpServants { get; }
    }
}
