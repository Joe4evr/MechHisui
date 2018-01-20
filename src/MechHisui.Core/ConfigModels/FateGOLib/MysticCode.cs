using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using MechHisui.FateGOLib;

namespace MechHisui.Core
{
    public sealed class MysticCode : IMysticCode
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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

        IEnumerable<IMysticAlias> IMysticCode.Aliases => Aliases;

        public override string ToString() => Code;
    }
}
