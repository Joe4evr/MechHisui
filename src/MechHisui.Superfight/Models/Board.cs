using System;
using System.Collections.Generic;
using System.Text;

namespace MechHisui.Superfight
{
    internal sealed class Board
    {
        public LocationCard Location { get; }

        public CharacterCard Fighter1 { get; }
        public ICollection<AbilityCard> Fighter1Abilities { get; } = new List<AbilityCard>();

        public CharacterCard Fighter2 { get; }
        public ICollection<AbilityCard> Fighter2Abilities { get; } = new List<AbilityCard>();

        public Board(LocationCard location, CharacterCard fighter1, CharacterCard fighter2)
        {
            Location = location;
            Fighter1 = fighter1;
            Fighter2 = fighter2;
        }

        //public override string ToString()
        //{
        //    return base.ToString();
        //}
    }
}
