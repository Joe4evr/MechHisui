using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MechHisui.FateGOLib;

namespace MechHisui.Core
{
    public sealed class ActiveSkill : IActiveSkill
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Name { get; set; }
        public string Rank { get; set; }
        public string Effect { get; set; }
        public string RankUpEffect { get; set; }

        public override string ToString() => $"{Name}: {Rank}";
    }


    public sealed class PassiveSkill : IPassiveSkill
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Name { get; set; }
        public string Rank { get; set; }
        public string Effect { get; set; }

        public override string ToString() => $"{Name}: {Rank}";
    }
}
