using System;

namespace MechHisui.Core
{
    public enum CardType
    {
        Character,
        Ability,
        Location
    }

    public class SuperfightCard
    {
        public int Id { get; set; }

        public CardType CardType { get; set; }
        public string Value { get; set; }
    }
}
