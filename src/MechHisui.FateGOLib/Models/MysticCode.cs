using System;
using System.Collections.Generic;

namespace MechHisui.FateGOLib
{
    public class MysticCode
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Skill1 { get; set; }
        public string Skill1Effect { get; set; }
        public string Skill2 { get; set; }
        public string Skill2Effect { get; set; }
        public string Skill3 { get; set; }
        public string Skill3Effect { get; set; }
        public string Image { get; set; }
        public ICollection<MysticAlias> Aliases { get; set; }
    }
}
