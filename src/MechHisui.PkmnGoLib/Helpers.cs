using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MechHisui.PkmnGoLib
{
    public static class PgoHelpers
    {
        public static List<Pokemon> KnownMons = new List<Pokemon>();
        internal static List<StardustLevel> StardustPerLevel = new List<StardustLevel>();
        internal static List<CP> CPMultiplier = new List<CP>();
    }
}
