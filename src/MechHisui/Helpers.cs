using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MechHisui
{
    internal static class Helpers
    {
        internal static long[] ConvertStringArrayToLongArray(params string[] strings)
        {
            var longs = new List<long>();
            foreach (var s in strings)
            {
                long temp;
                if (Int64.TryParse(s, out temp))
                {
                    longs.Add(temp);
                }
            }

            return longs.ToArray();
        }
    }
}
