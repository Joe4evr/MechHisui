﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace MechHisui.SymphoXDULib
{
    public interface IXduProfile
    {
        int StartId { get; }
        int Rarity { get; }
        int HP { get; }
        int Atk { get; }
        int Def { get; }
        int Spd { get; }
        int Ctr { get; }
        int Ctd { get; }
        string LeaderSkill { get; }
        string PassiveSkill { get; }
        IEnumerable<IXduSkill> Skills { get; }
        string Element { get; }
        string CharacterName { get; }
        string Image { get; }
        IEnumerable<string> Aliases { get; }
    }
}