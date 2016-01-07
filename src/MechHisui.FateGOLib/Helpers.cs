using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MechHisui.FateGOLib
{
    public static class FgoHelpers
    {
        public static readonly TimeZoneInfo JpnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");

        public static List<ServantProfile> ServantProfiles = new List<ServantProfile>();
        public static List<ServantProfile> FakeServantProfiles = new List<ServantProfile>();
        public static List<ServantAlias> ServantDict = new List<ServantAlias>();

        public static List<CEProfile> CEProfiles = new List<CEProfile>();
        public static List<CEAlias> CEDict = new List<CEAlias>();

        public static List<Event> EventList = new List<Event>();
        public static List<MysticCode> MysticCodeList = new List<MysticCode>();
        public static List<MysticAlias> MysticCodeDict = new List<MysticAlias>();

        public static List<NodeDrop> ItemDropsList = new List<NodeDrop>();

//        internal static void Test()
//        {
//FgoHelpers.ServantProfiles.Where(s => s.PassiveSkill1.Contains("Riding")).Select(s => new { Name = s.Name, Rank = s.PassiveRank1 }).Concat(
//FgoHelpers.ServantProfiles.Where(s => s.PassiveSkill2.Contains("Riding")).Select(s => new { Name = s.Name, Rank = s.PassiveRank2 })).Concat(
//FgoHelpers.ServantProfiles.Where(s => s.PassiveSkill3.Contains("Riding")).Select(s => new { Name = s.Name, Rank = s.PassiveRank3 })).Concat(
//FgoHelpers.ServantProfiles.Where(s => s.PassiveSkill4.Contains("Riding")).Select(s => new { Name = s.Name, Rank = s.PassiveRank4 }))
//.OrderByDescending(p => p.Rank, new RankComparer()).Select(p => $"{p.Name}: {p.Rank}");
//        }
    }
}
