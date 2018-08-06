using System;
using System.Collections.Generic;
using System.Linq;

namespace MechHisui.FateGOLib
{
    /// <summary> Comparer for Skill ranks. </summary>
    /// <remarks>"EX" > "A" > "B" > "C" etc.</remarks>
    public class RankComparer : Comparer<string>
    {
        private static readonly Func<char, bool> _plusses = c => c == '+';
        private static readonly Func<char, bool> _minusses = c => c == '-';

        public override int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (String.IsNullOrWhiteSpace(x)) return -1;
            if (String.IsNullOrWhiteSpace(y)) return 1;
            if (x == "EX") return 1;
            if (y == "EX") return -1;

            if (x.First() == y.First())
            {
                return (x.Count(_plusses) > y.Count(_plusses)
                    ^ x.Count(_minusses) < y.Count(_minusses)) ? 1 : -1;
            }
            else
            {
                return y.First().CompareTo(x.First());
            }
        }
    }
}
