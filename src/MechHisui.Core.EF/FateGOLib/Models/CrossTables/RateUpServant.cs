using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MechHisui.Core
{
    public sealed class RateUpServant
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public FgoEventGacha EventGacha { get; set; }
        public ServantProfile Servant { get; set; }
    }
}
