using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using MechHisui.FateGOLib;

namespace MechHisui.Core
{
    public sealed class ServantProfile : IServantProfile
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public ServantClass Class { get; set; }
        public int Rarity { get; set; }
        public string Name { get; set; }
        public int Atk { get; set; }
        public int HP { get; set; }
        public int Starweight { get; set; }
        public ServantGender Gender { get; set; }
        public ServantAttribute Attribute { get; set; }
        public ServantGrowthCurve GrowthCurve { get; set; }

        public ServantCardPool CardPool { get; set; }
        public int B { get; set; }
        public int A { get; set; }
        public int Q { get; set; }
        public int EX { get; set; }

        public FgoCard NPType { get; set; }
        public string NoblePhantasm { get; set; }
        public string NoblePhantasmEffect { get; set; }
        public string NoblePhantasmRankUpEffect { get; set; }

        public CEProfile Bond10 { get; set; }
        public string Additional { get; set; }
        public string Image { get; set; }
        public bool Obtainable { get; set; }

        public IEnumerable<ServantProfileTrait> Traits { get; set; }
        public IEnumerable<ServantActiveSkill> ActiveSkills { get; set; }
        public IEnumerable<ServantPassiveSkill> PassiveSkills { get; set; }
        public IEnumerable<ServantAlias> Aliases { get; set; }

        ICEProfile IServantProfile.Bond10 => Bond10;
        IEnumerable<string> IServantProfile.Traits => Traits.Select(t => t.Trait.Name);
        IEnumerable<IActiveSkill> IServantProfile.ActiveSkills => ActiveSkills.Select(s => s.Skill);
        IEnumerable<IPassiveSkill> IServantProfile.PassiveSkills => PassiveSkills.Select(s => s.Skill);
        IEnumerable<IServantAlias> IServantProfile.Aliases => Aliases;

        public override string ToString() => Name;
    }
}
