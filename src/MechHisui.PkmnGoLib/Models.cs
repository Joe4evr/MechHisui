using System;
using System.Collections.Generic;

namespace MechHisui.PkmnGoLib
{
    public class Mon
    {
        public DateTime Timestamp { get; set; }
        public ulong Owner { get; set; }
        public string Species { get; set; }
        public double Num { get; set; }
        public double HP { get; set; }
        public double CP { get; set; }
        public double DustPrice { get; set; }
        public List<IVs> PotentialIVs { get; set; }
    }

    public class IVs
    {
        public double Level { get; set; }
        public double AtkIV { get; set; }
        public double DefIV { get; set; }
        public double StaIV { get; set; }

        public double GetPercentage() => Math.Round(((AtkIV + DefIV + StaIV) / 45) * 100, 1);
    }

    public class Pokemon
    {
        public string Name { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Stamina { get; set; }
        public string Evolution { get; set; }
    }

    public class StardustLevel
    {
        public int Stardust { get; set; }
        public int Level { get; set; }
    }

    public class CP
    {
        public double Level { get; set; }
        public double CpMultiplier { get; set; }
    }
}
