using System;

namespace MechHisui.SymphoXDULib
{
    public class XduEvent
    {
        //public int Id { get; set; }
        public string EventName { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string EventGacha { get; set; }
        public string InfoLink { get; set; }
    }
}
