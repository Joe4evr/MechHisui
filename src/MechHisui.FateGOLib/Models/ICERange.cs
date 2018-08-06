using System;
using System.Collections.Generic;
using System.Text;

namespace MechHisui.FateGOLib
{
    public interface ICERange
    {
        int LowId { get; }
        int HighId { get; }

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
    }
}
