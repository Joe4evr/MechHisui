using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using JiiLib;

namespace MechHisui.FateGOLib
{
    public static class FgoHelpers
    {
        public static readonly TimeZoneInfo JpnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
        public static readonly TimeSpan PerAP = TimeSpan.FromMinutes(5);

        public static List<ServantProfile> ServantProfiles = new List<ServantProfile>();
        public static List<ServantProfile> FakeServantProfiles = new List<ServantProfile>();
        public static List<ServantAlias> ServantDict = new List<ServantAlias>();

        public static List<CEProfile> CEProfiles = new List<CEProfile>();
        public static List<CEAlias> CEDict = new List<CEAlias>();

        public static List<Event> EventList = new List<Event>();
        public static List<MysticCode> MysticCodeList = new List<MysticCode>();
        public static List<MysticAlias> MysticCodeDict = new List<MysticAlias>();

        public static List<NodeDrop> ItemDropsList = new List<NodeDrop>();
        public static List<string> Masters = new List<string>();
        public static List<NameOnlyServant> NameOnlyServants = new List<NameOnlyServant>();

        public static List<UserAP> UsersAP = new List<UserAP>();

        public static void InitRandomHgw(IConfiguration config)
        {
            using (TextReader tr = new StreamReader(Path.Combine(config["other"], "masters.json")))
            {
                Masters = JsonConvert.DeserializeObject<List<string>>(tr.ReadToEnd());
            }
            using (TextReader tr = new StreamReader(Path.Combine(config["other"], "nameonlyservants.json")))
            {
                NameOnlyServants = JsonConvert.DeserializeObject<List<NameOnlyServant>>(tr.ReadToEnd());
            }
        }

        public static IEnumerable<ServantProfile> WhereActive(this IEnumerable<ServantProfile> profiles, string skill)
            => profiles.Where(p => p.ActiveSkills.Any(s => s.SkillName == skill));

        public static IEnumerable<ServantProfile> WhereActiveEffect(this IEnumerable<ServantProfile> profiles, string effect)
            => profiles.Where(p => p.ActiveSkills.Any(s => s.Effect.Contains(effect)));

        public static IEnumerable<ServantProfile> WherePassive(this IEnumerable<ServantProfile> profiles, string skill)
            => profiles.Where(p => p.PassiveSkills.Any(s => s.SkillName == skill));

        public static IEnumerable<ServantProfile> WherePassiveEffect(this IEnumerable<ServantProfile> profiles, string effect)
            => profiles.Where(p => p.PassiveSkills.Any(s => s.Effect.Contains(effect)));

        public static IEnumerable<ServantProfile> WhereTrait(this IEnumerable<ServantProfile> profiles, string trait)
            => profiles.Where(p => p.Traits.ContainsIgnoreCase(trait));

        //private static string test()
        //{
        //    
        //}
    }
}
