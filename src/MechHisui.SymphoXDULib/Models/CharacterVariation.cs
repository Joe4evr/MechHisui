using System;
using System.Collections.Generic;
using System.Text;

namespace MechHisui.SymphoXDULib
{
    public class CharacterVariation
    {
        public int Id { get; set; }
        public int HP { get; set; }
        public int Atk { get; set; }
        public int Def { get; set; }
        public int Spd { get; set; }
        public int Ctr { get; set; }
        public int Ctd { get; set; }
        public string LeaderSkill { get; set; }
        public string PassiveSkill { get; set; }
        public IList<Skill> Skills { get; set; }
    }
}
