using System;
using System.Collections.Generic;

namespace MechHisui.FateGOLib
{
    public class ServantProfile
    {
        public int Id { get; set; }
        public string Class { get; set; }
        public int Rarity { get; set; }
        public string Name { get; set; }
        public int Atk { get; set; }
        public int HP { get; set; }
        public int Starweight { get; set; }
        public string Gender { get; set; }
        public string GrowthCurve { get; set; }
        public string CardPool { get; set; }
        public int B { get; set; }
        public int A { get; set; }
        public int Q { get; set; }
        public int EX { get; set; }
        public string NPType { get; set; }
        public string NoblePhantasm { get; set; }
        public string NoblePhantasmEffect { get; set; }
        public string NoblePhantasmRankUpEffect { get; set; }
        //public ICollection<string> Traits { get; set; }
        public ICollection<ServantTrait> Traits { get; set; }
        public string Attribute { get; set; }
        public ICollection<ServantSkill> ActiveSkills { get; set; }
        public ICollection<ServantSkill> PassiveSkills { get; set; }
        public string Additional { get; set; }
        public string Image { get; set; }
        public bool Obtainable { get; set; }
        //public ICollection<string> Aliases { get; set; }
        public ICollection<ServantAlias> Aliases { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
