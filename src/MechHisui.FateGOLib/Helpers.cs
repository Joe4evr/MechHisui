using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using JiiLib;

namespace MechHisui.FateGOLib
{
    public static class FgoHelpers
    {
        private static readonly Platform platform = PlatformServices.Default.Runtime.OperatingSystemPlatform;
        public static TimeZoneInfo JpnTimeZone
            {
                get
                {
                    return platform == Platform.Windows
                        ? TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time")
                        : TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
                }
            }
        public static readonly TimeSpan PerAP = TimeSpan.FromMinutes(5);

        public static List<ServantProfile> ServantProfiles = new List<ServantProfile>();
        public static List<ServantProfile> FakeServantProfiles = new List<ServantProfile>();
        public static Dictionary<string, string> ServantDict = new Dictionary<string, string>();

        public static List<CEProfile> CEProfiles = new List<CEProfile>();
        public static Dictionary<string, string> CEDict = new Dictionary<string, string>();

        public static List<Event> EventList = new List<Event>();
        public static List<MysticCode> MysticCodeList = new List<MysticCode>();
        public static Dictionary<string, string> MysticCodeDict = new Dictionary<string, string>();

        public static List<NodeDrop> ItemDropsList = new List<NodeDrop>();
        public static List<string> Masters = new List<string>();
        public static List<NameOnlyServant> NameOnlyServants = new List<NameOnlyServant>();

        public const int MaxAP = 140;
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

        private static void F()
        {
            var x = FgoHelpers.ServantProfiles.Where(p => p.Rarity <= 3).SelectMany(p => p.ActiveSkills.Where(s => s.Effect.Contains("Heal")).Select(s => $"{p.Name} - {s.SkillName}: {s.Effect}"));
        }
    }
}
