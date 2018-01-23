﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MechHisui.Core
{
    public sealed class ServantActiveSkill
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public ServantProfile Servant { get; set; }
        public ActiveSkill Skill { get; set; }

        public override string ToString() => $"{Servant.ToString()} - {Skill.ToString()}";
    }

    public sealed class ServantPassiveSkill
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public ServantProfile Servant { get; set; }
        public PassiveSkill Skill { get; set; }

        public override string ToString() => $"{Servant.ToString()} - {Skill.ToString()}";
    }

}