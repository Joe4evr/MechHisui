﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace MechHisui
{

    public sealed class DiceRoll
    {
        public int Amount { get; }
        public int Sides { get; }

        public DiceRoll(int amount, int sides)
        {
            Amount = amount;
            Sides = sides;
        }

        public IEnumerable<int> Roll()
        {
            var rng = new Random();
            var dice = Enumerable.Range(1, Sides);
            for (int i = 0; i < Amount; i++)
            {
                dice = dice.Shuffle(28);
                yield return dice.ElementAt(rng.Next(maxValue: Sides));
            }
        }
    }
}
