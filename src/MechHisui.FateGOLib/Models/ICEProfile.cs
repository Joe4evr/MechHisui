using System.Collections.Generic;

namespace MechHisui.FateGOLib
{
    public interface ICEProfile
    {
        int Id { get; }
        int Rarity { get; }
        string Name { get; }
        int Cost { get; }

        int Atk { get; }
        int HP { get; }
        string Effect { get; }
        string EventEffect { get; }

        int AtkMax { get; }
        int HPMax { get; }
        string EffectMax { get; }
        string EventEffectMax { get; }

        string Image { get; }
        bool Obtainable { get; }

        IEnumerable<ICEAlias> Aliases { get; }
    }
}