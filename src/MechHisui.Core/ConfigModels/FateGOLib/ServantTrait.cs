using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MechHisui.FateGOLib;

namespace MechHisui.Core
{
    public sealed class ServantTrait : IServantTrait
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Trait { get; set; }
        public bool IsAutoComputed { get; set; }

        public override string ToString() => Trait;
    }
}
