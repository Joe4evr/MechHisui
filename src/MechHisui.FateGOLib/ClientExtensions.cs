using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using MechHisui.FateGOLib;

namespace MechHisui.Commands
{
    public static class ClientExtensions
    {
        public static void RegisterDailyCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Daily'...");
            client.Commands().CreateCommand("daily")
                .AddCheck((c, u, ch) => ch.Id == Int64.Parse(config["FGO_general"]))
                .Parameter("day", ParameterType.Optional)
                .Description("Relay the information of daily quests for the specified day. Default to current day.")
                .Do(async cea =>
                {
                    DayOfWeek day;
                    DateTimeWithZone todayInJapan = new DateTimeWithZone(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time"));
                    var arg = cea.Args[0];

                    if (String.IsNullOrWhiteSpace(arg))
                    {
                        day = todayInJapan.LocalTime.DayOfWeek;
                    }
                    else if (arg == "tomorrow")
                    {
                        day = todayInJapan.LocalTime.AddDays(1).DayOfWeek;
                    }
                    else if (!Enum.TryParse(arg, ignoreCase: true, result: out day))
                    {
                        await client.SendMessage(cea.Channel, "Could not convert argument to a day of the week. Please try again.");
                        return;
                    }

                    DailyInfo info;
                    if (DailyInfo.DailyQuests.TryGetValue(day, out info))
                    {
                        bool isToday = (day == todayInJapan.LocalTime.DayOfWeek);
                        bool isTomorrow = (day == todayInJapan.LocalTime.AddDays(1).DayOfWeek);
                        string whatDay = isToday ? "Today" : (isTomorrow ? "Tomorrow" : day.ToString());
                        if (day != DayOfWeek.Sunday)
                        {
                            await client.SendMessage(cea.Channel, $"{whatDay}'s quests:\n\tAscension Materials: **{info.Materials.ToString()}**\n\tExperience: **{info.Exp1.ToString()}**, **{info.Exp2.ToString()}**, and **{ServantClass.Berzerker.ToString()}**");
                        }
                        else
                        {
                            await client.SendMessage(cea.Channel, $"{whatDay}'s quests:\n\tAscension Materials: **{info.Materials.ToString()}**\n\tAnd also **QP**");
                        }
                    }
                });
        }

        public static void RegisterFriendsCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Friends'...");
            FriendCodes.ReadFriendData(config["FriendcodePath"]);
            client.Commands().CreateCommand("friendcode")
               .AddCheck((c, u, ch) => ch.Id == long.Parse(config["FGO_general"]))
               .Parameter("code", ParameterType.Required)
               .Parameter("servant", ParameterType.Optional)
               .Description("Add your friendcode to the list. Enter your code with quotes as `\"XXX XXX XXX\"`. You may add your support Servant as well.")
               .Do(async cea =>
               {
                   if (Regex.Match(cea.Args[0], @"[0-9][0-9][0-9] [0-9][0-9][0-9] [0-9][0-9][0-9]").Success)
                   {
                       var friend = new FriendData { User = cea.User.Name, FriendCode = cea.Args[0], Servant = (cea.Args.Length > 1) ? cea.Args[1] : String.Empty };
                       FriendCodes.friendData.Add(friend);
                       FriendCodes.WriteFriendData(config["FriendcodePath"]);
                       await client.SendMessage(cea.Channel, $"Added {friend.FriendCode} for {friend.User}.");
                   }
                   else
                   {
                       await client.SendMessage(cea.Channel, $"Incorrect friendcode format specified.");
                   }
               });
            client.Commands().CreateCommand("listcodes")
               .AddCheck((c, u, ch) => ch.Id == long.Parse(config["FGO_general"]))
               .Description("Display known friendcodes.")
               .Do(async cea =>
               {
                   StringBuilder sb = new StringBuilder();
                   foreach (var friend in FriendCodes.friendData)
                   {
                       sb.Append($"{friend.User}: {friend.FriendCode}");
                       sb.AppendLine((!String.IsNullOrEmpty(friend.Servant)) ? $" - {friend.Servant}" : String.Empty);
                   }
                   await client.SendMessage(cea.Channel, sb.ToString());
               });
        }

        public static void RegisterQuartzCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Quartz'...");
            client.Commands().CreateCommand("quartz")
                .AddCheck((c, u, ch) => ch.Id == Int64.Parse(config["FGO_general"]))
                .Description("Relay the prices of different amounts of Saint Quartz.")
                .Do(async cea =>
                {
                    await client.SendMessage(cea.Channel, "Prices for Quartz:\n  1Q: 120 JPY\n  5Q: 480 JPY\n 16Q: 1400 JPY\n 36Q: 2900 JPY\n 65Q: 4800 JPY\n140Q: 9800 JPY");
                });
        }

        public static void RegisterStatsCommand(this DiscordClient client, IConfiguration config, StatService statService)
        {
            Console.WriteLine("Registering 'Stats'...");
            client.Commands().CreateCommand("stats")
                .AddCheck((c, u, ch) => ch.Id == Int64.Parse(config["FGO_general"]))
                .Parameter("servantname", ParameterType.Multiple)
                .Description($"Relay information on the specified Servant. Alternative names acceptable. *Currently up to {FgoHelpers.ServantProfiles.Count(p => !String.IsNullOrWhiteSpace(p.NoblePhantasm))}/{FgoHelpers.ServantProfiles.Count}.*")
                .Do(async cea =>
                {
                    var arg = String.Join(" ", cea.Args);
                    if (new[] { "waifu", "mai waifu", "my waifu" }.Contains(arg.ToLowerInvariant()))
                    {
                        await client.SendMessage(cea.Channel, "It has come to my attention that your 'waifu' is equatable to fecal matter.");
                        return;
                    }

                    //if (new[] { "" }.Contains(arg.ToLowerInvariant()))
                    //{
                    //    await client.SendMessage(cea.Channel, "Never ever.");
                    //    return;
                    //}

                    var profile = statService.LookupStats(arg);
                    if (profile != null)
                    {
                        await client.SendMessage(cea.Channel, FormatServantProfile(profile)); 
                    }
                    else
                    {
                        var name = statService.LookupServantName(arg);
                        if (name != null)
                        {
                            await client.SendMessage(cea.Channel, $"**Servant:** {name}\nMore information TBA.");
                        }
                        else
                        {
                            await client.SendMessage(cea.Channel, "No such entry found. Please try another name.");
                        }
                    }
                });

            Console.WriteLine("Registering 'Update'...");
            client.Commands().CreateCommand("update")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && (ch.Id == Int64.Parse(config["FGO_general"])))
                .Parameter("item", ParameterType.Optional)
                .Hide()
                .Do(async cea =>
                {
                    switch (cea.Args[0])
                    {
                        case "alias":
                            statService.ReadAliasList();
                            await client.SendMessage(cea.Channel, "Updated alias lookup.");
                            break;
                        case "profiles":
                            await statService.UpdateProfileListsAsync();
                            await client.SendMessage(cea.Channel, "Updated profile lookup.");
                            break;
                        default:
                            statService.ReadAliasList();
                            await statService.UpdateProfileListsAsync();
                            await client.SendMessage(cea.Channel, "Updated all lookups.");
                            break;
                    }
                });

            Console.WriteLine("Registering 'Add alias'...");
            client.Commands().CreateCommand("addalias")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && ch.Id == Int64.Parse(config["FGO_general"]))
                .Hide()
                .Parameter("servant", ParameterType.Required)
                .Parameter("alias", ParameterType.Required)
                .Do(async cea =>
                {
                    var newAlias = FgoHelpers.ServantDict.SingleOrDefault(p => p.Servant == cea.Args[0]);
                    if (newAlias != null)
                    {
                        newAlias.Alias.Add(cea.Args[1]);
                        using (TextWriter tw = new StreamWriter(config["ServantAliasPath"]))
                        {
                            tw.Write(JsonConvert.SerializeObject(FgoHelpers.ServantDict, Formatting.Indented));
                        }
                        await client.SendMessage(cea.Channel, $"Added alias `{cea.Args[1]}` for `{newAlias.Servant}`.");
                    }
                    else
                    {
                        await client.SendMessage(cea.Channel, "Could not find name to add alias for.");
                    }
                });
        }

        internal static string FormatServantProfile(ServantProfile profile)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"**Servant:** {profile.Name}")
                .AppendLine($"**Class:** {profile.Class}")
                .AppendLine($"**Rarity:** {profile.Rarity}")
                .AppendLine($"**Collection ID:** {profile.Id}")
                .AppendLine($"**Card pool:** {profile.CardPool}")
                .AppendLine($"**Max ATK:** {profile.Atk}")
                .AppendLine($"**Max HP:** {profile.HP}")
                .AppendLine($"**Growth type:** {profile.GrowthCurve}")
                .AppendLine($"**NP:** {profile.NoblePhantasm} - *{profile.NoblePhantasmEffect}*");
            if (!String.IsNullOrWhiteSpace(profile.Skill1))
            {
                sb.AppendLine($"**Skill 1:** {profile.Skill1} - *{profile.Effect1}*");
            }
            if (!String.IsNullOrWhiteSpace(profile.Skill2))
            {
                sb.AppendLine($"**Skill 2:** {profile.Skill2} - *{profile.Effect2}*");
            }
            if (!String.IsNullOrWhiteSpace(profile.Skill3))
            {
                sb.AppendLine($"**Skill 3:** {profile.Skill3} - *{profile.Effect3}*");
            }
            if (!String.IsNullOrWhiteSpace(profile.PassiveSkill1))
            {
                sb.AppendLine($"**Passive 1:** {profile.PassiveSkill1} - *{profile.PEffect1}*");
                if (!String.IsNullOrWhiteSpace(profile.PassiveSkill2))
                {
                    sb.AppendLine($"**Passive 2:** {profile.PassiveSkill2} - *{profile.PEffect2}*");
                    if (!String.IsNullOrWhiteSpace(profile.PassiveSkill3))
                    {
                        sb.AppendLine($"**Passive 3:** {profile.PassiveSkill3} - *{profile.PEffect3}*");
                        if (!String.IsNullOrWhiteSpace(profile.PassiveSkill4))
                        {
                            sb.AppendLine($"**Passive 4:** {profile.PassiveSkill4} - *{profile.PEffect4}*");
                        }
                    }
                }
            }
            sb.AppendLine($"{profile.Image}");
            return sb.ToString();
        }
    }

    internal struct DateTimeWithZone
    {
        private readonly DateTime utcDateTime;
        private readonly TimeZoneInfo timeZone;

        public DateTimeWithZone(DateTime dateTimeUtc, TimeZoneInfo timeZone)
        {
            utcDateTime = dateTimeUtc;
            this.timeZone = timeZone;
        }

        public DateTime UniversalTime => utcDateTime;

        public TimeZoneInfo TimeZone => timeZone;

        public DateTime LocalTime => TimeZoneInfo.ConvertTime(utcDateTime, timeZone);
    }
}
