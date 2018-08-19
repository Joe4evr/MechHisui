using System;
using System.Collections.Generic;

namespace MechHisui.FateGOLib
{
    public class DailyInfo
    {
        public ServantClass Materials { get; set; }
        public ServantClass Exp1 { get; set; }
        public ServantClass Exp2 { get; set; }

        public static IReadOnlyDictionary<DayOfWeek, DailyInfo> DailyQuests = new Dictionary<DayOfWeek, DailyInfo>
        {
            [DayOfWeek.Monday] = new DailyInfo { Materials = ServantClass.Archer, Exp1 = ServantClass.Lancer, Exp2 = ServantClass.Assassin },
            [DayOfWeek.Tuesday] = new DailyInfo { Materials = ServantClass.Lancer, Exp1 = ServantClass.Saber, Exp2 = ServantClass.Rider },
            [DayOfWeek.Wednesday] = new DailyInfo { Materials = ServantClass.Berserker, Exp1 = ServantClass.Archer, Exp2 = ServantClass.Caster },
            [DayOfWeek.Thursday] = new DailyInfo { Materials = ServantClass.Rider, Exp1 = ServantClass.Lancer, Exp2 = ServantClass.Assassin },
            [DayOfWeek.Friday] = new DailyInfo { Materials = ServantClass.Caster, Exp1 = ServantClass.Saber, Exp2 = ServantClass.Rider },
            [DayOfWeek.Saturday] = new DailyInfo { Materials = ServantClass.Assassin, Exp1 = ServantClass.Archer, Exp2 = ServantClass.Caster },
            [DayOfWeek.Sunday] = new DailyInfo { Materials = ServantClass.Saber }
        };
    }
}
