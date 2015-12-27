using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MechHisui.FateGOLib
{
    public static class FgoHelpers
    {
        public static List<ServantProfile> ServantProfiles = new List<ServantProfile>();
        public static List<ServantProfile> FakeServantProfiles = new List<ServantProfile>();
        public static List<ServantAlias> ServantDict = new List<ServantAlias>();

        public static List<CEProfile> CEProfiles = new List<CEProfile>();
        public static List<CEAlias> CEDict = new List<CEAlias>();

        public static List<Event> EventList = new List<Event>();
        public static List<MysticCode> MysticCodeList = new List<MysticCode>();
        public static List<MysticAlias> MysticCodeDict = new List<MysticAlias>();
    }
}
