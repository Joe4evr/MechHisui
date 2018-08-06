using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MechHisui.FateGOLib;

namespace MechHisui.Core
{
    public sealed class FgoEvent : IFgoEvent
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string EventName { get; set; }
        public DateTimeOffset? StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }
        public IEnumerable<FgoEventGacha> EventGachas { get; set; }
        public string InfoLink { get; set; }

        IEnumerable<IFgoEventGacha> IFgoEvent.EventGachas => EventGachas;
    }
}
