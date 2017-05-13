using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace MechHisui.FateGOLib
{
    internal static class FgoHelpers
    {
        public static readonly TimeZoneInfo JpnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
        public static readonly TimeSpan PerAP = TimeSpan.FromMinutes(5);

        //public static List<ServantProfile> ServantProfiles = new List<ServantProfile>();
        //public static List<ServantProfile> FakeServantProfiles = new List<ServantProfile>();
        //public static Dictionary<string, string> ServantDict = new Dictionary<string, string>();

        //public static List<CEProfile> CEProfiles = new List<CEProfile>();
        //public static Dictionary<string, string> CEDict = new Dictionary<string, string>();

        //public static List<Event> EventList = new List<Event>();
        //public static List<MysticCode> MysticCodeList = new List<MysticCode>();
        //public static Dictionary<string, string> MysticCodeDict = new Dictionary<string, string>();

        //public static List<NodeDrop> ItemDropsList = new List<NodeDrop>();
        //public static List<string> Masters = new List<string>();
        //public static List<NameOnlyServant> NameOnlyServants = new List<NameOnlyServant>();

        //public const int MaxAP = 140;
        //public static List<UserAP> UsersAP = new List<UserAP>();

        //public static void InitRandomHgw(FgoConfig config)
        //{
        //    Masters = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(config.MasterNamesPath));
        //    NameOnlyServants = JsonConvert.DeserializeObject<List<NameOnlyServant>>(File.ReadAllText(config.NameOnlyServantsPath));
        //}
    }
}
