using System;
using System.Collections.Generic;

namespace MechHisui.FateGOLib
{
    public interface IFgoEvent
    {
        int Id { get; }
        string EventName { get; }
        DateTimeOffset? StartTime { get; }
        DateTimeOffset? EndTime { get; }
        IEnumerable<IFgoEventGacha> EventGachas { get; }
        string InfoLink { get; }
    }
}