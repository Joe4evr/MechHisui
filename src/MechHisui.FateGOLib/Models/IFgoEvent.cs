using System;

namespace MechHisui.FateGOLib
{
    public interface IFgoEvent
    {
        DateTime? EndTime { get; }
        string EventGacha { get; }
        string EventName { get; }
        string InfoLink { get; }
        DateTime? StartTime { get; }
    }
}