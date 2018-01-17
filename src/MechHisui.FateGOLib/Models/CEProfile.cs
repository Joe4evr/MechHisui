using System;
using System.Collections.Generic;

namespace MechHisui.FateGOLib
{
    public class CEProfile
    {
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

        public override string ToString()
        {
            return Name;
        }
    }
}
