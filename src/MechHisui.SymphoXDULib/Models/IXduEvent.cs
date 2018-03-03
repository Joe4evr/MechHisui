using System;

namespace MechHisui.SymphoXDULib
{
    public interface IXduEvent
    {
        string EventName { get; }
        DateTime? StartTime { get; }
        DateTime? EndTime { get; }
        string EventGacha { get; }
        string InfoLink { get; }
    }
}
