using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MechHisui.FateGOLib;

#nullable disable warnings
namespace MechHisui.Core
{
    public sealed class ServantTrait : IServantTrait
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Trait { get; set; }
        public bool IsAutoComputed { get; set; }
        //public IEnumerable<ServantProfileTrait> Servants { get; set; }

        public override string ToString() => Trait;
    }
}
