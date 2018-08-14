using System;
using System.Collections.Generic;
using System.Linq;
using SharedExtensions;

namespace MechHisui.Core
{
    public sealed class DiceRoll
    {
        public int Amount { get; }
        public int Sides { get; }
        public bool IsNegative { get; }

        public DiceRoll(int amount, int sides, bool isNegative)
        {
            Amount = amount;
            Sides = sides;
            IsNegative = isNegative;
        }

        public IEnumerable<int> Roll(Random rng)
        {
            var dice = Enumerable.Range(1, Sides);
            for (int i = 0; i < Amount; i++)
            {
                dice = dice.Shuffle(28);
                yield return dice.ElementAt(rng.Next(maxValue: Sides));
            }
        }
    }
}
