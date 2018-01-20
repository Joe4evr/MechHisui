using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using MechHisui.FateGOLib;

namespace MechHisui.Core
{
    public sealed class CEProfile : ICEProfile
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public int Rarity { get; set; }
        public string Name { get; set; }
        public int Cost { get; set; }
        public int Atk { get; set; }
        public int HP { get; set; }
        public string Effect { get; set; }
        public string EventEffect { get; set; }
        public int AtkMax { get; set; }
        public int HPMax { get; set; }
        public string EffectMax { get; set; }
        public string EventEffectMax { get; set; }
        public string Image { get; set; }
        public bool Obtainable { get; set; }
        public ICollection<CEAlias> Aliases { get; set; }

        IEnumerable<ICEAlias> ICEProfile.Aliases => Aliases;

        public override string ToString()
        {
            return Name;
        }
    }
}
