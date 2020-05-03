﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MechHisui.FateGOLib;

#nullable disable warnings
namespace MechHisui.Core
{
    public sealed class ActiveSkill : IActiveSkill
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string SkillName { get; set; }
        public string Rank { get; set; }
        public string Effect { get; set; }
        public string RankUpEffect { get; set; }

        //public IEnumerable<ServantActiveSkill> Servants { get; set; }

        public override string ToString() => $"{SkillName}: {Rank}";
    }


    public sealed class PassiveSkill : IPassiveSkill
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string SkillName { get; set; }
        public string Rank { get; set; }
        public string Effect { get; set; }

        //public ICollection<ServantPassiveSkill> Servants { get; set; }

        public override string ToString() => $"{SkillName}: {Rank}";
    }
}
