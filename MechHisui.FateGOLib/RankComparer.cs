using System;
using System.Collections.Generic;
using System.Linq;

namespace MechHisui.FateGOLib
{
    /// <summary>
    /// Comparer for Skill ranks.
    /// </summary>
    /// <remarks>"EX" > "A" > "B" > "C" etc.</remarks>
    public class RankComparer : Comparer<string>
    {
        public override int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (String.IsNullOrWhiteSpace(x)) return -1;
            if (String.IsNullOrWhiteSpace(y)) return 1;
            if (x == "EX") return 1;
            if (y == "EX") return -1;
            


            if (x.First() == y.First())
            {
                Func<char, bool> plusses = c => c == '+';
                Func<char, bool> minusses = c => c == '-';
                return (x.Count(plusses) > y.Count(plusses) ^
                    x.Count(minusses) < y.Count(minusses)) ? 1 : -1;
            }
            else
            {
                return y.First().CompareTo(x.First());
            }
        }
    }
}
