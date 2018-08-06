using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using MechHisui.FateGOLib;

namespace MechHisui.Core
{
    public sealed class FgoEventGacha : IFgoEventGacha
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public FgoEvent Event     { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime   { get; set; }
        public IEnumerable<RateUpServant> RateUpServants { get; set; }

        IEnumerable<IServantProfile> IFgoEventGacha.RateUpServants => RateUpServants.Select(r => r.Servant);
    }
}
