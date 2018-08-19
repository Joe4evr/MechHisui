using System.Collections.Generic;

namespace MechHisui.FateGOLib
{
    public interface IServantProfile
    {
        int Id { get; }
        ServantClass Class { get; }
        int Rarity { get; }
        string Name { get; }
        int Atk { get; }
        int HP { get; }
        int Starweight { get; }
        ServantGender Gender { get; }
        ServantAttribute Attribute { get; }
        ServantGrowthCurve GrowthCurve { get; }

        ServantCardPool CardPool { get; }
        int B { get; }
        int A { get; }
        int Q { get; }
        int EX { get; }

        FgoCard NPType { get; }
        string NoblePhantasm { get; }
        string NoblePhantasmEffect { get; }
        string NoblePhantasmRankUpEffect { get; }

        ICEProfile Bond10 { get; }
        string Additional { get; }
        string Image { get; }
        bool Obtainable { get; }

        IEnumerable<string> Traits { get; }
        IEnumerable<IActiveSkill> ActiveSkills { get; }
        IEnumerable<IPassiveSkill> PassiveSkills { get; }
        IEnumerable<IServantAlias> Aliases { get; }
    }
}