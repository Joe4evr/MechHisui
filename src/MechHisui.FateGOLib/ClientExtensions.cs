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
        private static readonly TimeZoneInfo JpnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
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

                    if (String.IsNullOrWhiteSpace(arg) || arg == "today")
                    {
                        day = todayInJapan.LocalTime.DayOfWeek;
                    }
                    else if (arg == "change")
                    {
                        var eta = todayInJapan.TimeUntilNextLocalTimeAt(new TimeSpan(hours: 0, minutes: 0, seconds: 0));
                        await client.SendMessage(cea.Channel, $"Daily quests changing **ETA: {eta.Hours} hours and {eta.Minutes} minutes.**");
                        return;
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

        public static void RegisterEventCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Event'...");
            client.Commands().CreateCommand("event")
                .AddCheck((c, u, ch) => ch.Id == Int64.Parse(config["FGO_general"]))
                .Description($"Relay information on current or upcoming events.")
                .Do(async cea =>
                {
                    StringBuilder sb = new StringBuilder();
                    var utcNow = DateTime.UtcNow;
                    var currentEvents = FgoHelpers.EventList.Where(e => utcNow > e.StartTime && utcNow < e.EndTime);
                    if (currentEvents.Any())
                    {
                        sb.Append("**Current Event(s):** ");
                        foreach (var ev in currentEvents)
                        {
                            var doneAt = ev.EndTime - utcNow;
                            string d = doneAt.Hours == 1 ? "day" : "days";
                            string h = doneAt.Hours == 1 ? "hour" : "hours";
                            string m = doneAt.Minutes == 1 ? "minute" : "minutes";
                            if (doneAt < TimeSpan.FromDays(1))
                            {
                                sb.Append($"{ev.EventName} for {doneAt.Hours} {h} and {doneAt.Minutes} {m}.");
                            }
                            else
                            {
                                sb.Append($"{ev.EventName} for {doneAt.Days} {d} and {doneAt.Hours} {h}.");
                            }

                            if (ev != currentEvents.Last())
                            {
                                sb.Append(", ");
                            }
                            else
                            {
                                sb.AppendLine();
                            }
                        }
                    }
                    else
                    {
                        sb.AppendLine("No events currently going on.");
                    }

                    var nextEvent = FgoHelpers.EventList.FirstOrDefault(e => e.StartTime > utcNow);
                    if (nextEvent != null)
                    {
                        var eta = nextEvent.StartTime - utcNow;
                        string d = eta.Hours == 1 ? "day" : "days";
                        string h = eta.Hours == 1 ? "hour" : "hours";
                        string m = eta.Minutes == 1 ? "minute" : "minutes";
                        if (eta < TimeSpan.FromDays(1))
                        {
                            sb.Append($"**Next Event:** {nextEvent.EventName}, planned to start in {eta.Hours} {h} and {eta.Minutes} {m}.");
                        }
                        else
                        {
                            sb.Append($"**Next Event:** {nextEvent.EventName}, planned to start in {eta.Days} {d} and {eta.Hours} {h}.");
                        }
                    }
                    else
                    {
                        sb.Append("No known upcoming events.");
                    }
                    await client.SendMessage(cea.Channel, sb.ToString());
                });
        }

        public static void RegisterFriendsCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Friends'...");
            FriendCodes.ReadFriendData(config["FriendcodePath"]);
            client.Commands().CreateCommand("friendcode")
               .AddCheck((c, u, ch) => ch.Id == Int64.Parse(config["FGO_general"]))
               .Parameter("code", ParameterType.Required)
               .Parameter("servant", ParameterType.Optional)
               .Description("Add your friendcode to the list. Enter your code with quotes as `\"XXX XXX XXX\"`. You may add your support Servant as well.")
               .Do(async cea =>
               {
                   if (FriendCodes.friendData.Any(fc => fc.User == cea.User.Name))
                   {
                       await client.SendMessage(cea.Channel, $"Already in the Friendcode list. Please use `.updatefc` to update your description.");
                       return;
                   }

                   if (Regex.Match(cea.Args[0], @"[0-9][0-9][0-9] [0-9][0-9][0-9] [0-9][0-9][0-9]").Success)
                   {
                       var friend = new FriendData { User = cea.User.Name, FriendCode = cea.Args[0], Servant = (cea.Args.Length > 1) ? cea.Args[1] : String.Empty };
                       FriendCodes.friendData.Add(friend);
                       FriendCodes.WriteFriendData(config["FriendcodePath"]);
                       await client.SendMessage(cea.Channel, $"Added `{friend.FriendCode}` for `{friend.User}`.");
                   }
                   else
                   {
                       await client.SendMessage(cea.Channel, $"Incorrect friendcode format specified.");
                   }
               });

            client.Commands().CreateCommand("listcodes")
               .AddCheck((c, u, ch) => ch.Id == Int64.Parse(config["FGO_general"]))
               .Description("Display known friendcodes.")
               .Do(async cea =>
               {
                   StringBuilder sb = new StringBuilder("```\n");
                   int longestName = FriendCodes.friendData.OrderByDescending(f => f.User.Length).First().User.Length;
                   foreach (var friend in FriendCodes.friendData)
                   {
                       string spaces = new string(' ', (longestName - friend.User.Length) + 1);
                       sb.Append($"{friend.User}:{spaces}{friend.FriendCode}");
                       sb.AppendLine((!String.IsNullOrEmpty(friend.Servant)) ? $" - {friend.Servant}" : String.Empty);
                   }
                   sb.Append("\n```");
                   await client.SendMessage(cea.Channel, sb.ToString());
               });

            client.Commands().CreateCommand("updatefc")
               .AddCheck((c, u, ch) => ch.Id == long.Parse(config["FGO_general"]))
               .Parameter("newServant", ParameterType.Multiple)
               .Description("Update the Support Servant displayed in your friendcode listing.")
               .Do(async cea =>
               {
                   if (!cea.Args.Any())
                   {
                       await client.SendMessage(cea.Channel, $"No argument specified.");
                       return;
                   }

                   var arg = string.Join(" ", cea.Args);
                   Func<FriendData, bool> pred = c => c.User == cea.User.Name;
                   if (FriendCodes.friendData.Any(pred))
                   {
                       var temp = FriendCodes.friendData.Single(pred);
                       FriendCodes.friendData.Remove(FriendCodes.friendData.Single(pred));
                       temp.Servant = arg;
                       FriendCodes.friendData.Add(temp);
                       FriendCodes.WriteFriendData(config["FriendcodePath"]);
                       await client.SendMessage(cea.Channel, $"Updated `{temp.User}`'s Servant to be `{temp.Servant}`.");
                   }
                   else
                   {
                       await client.SendMessage(cea.Channel, "Profile not found. Please add your profile using `.friendcode`.");
                   }
               });
        }

        public static void RegisterLoginBonusCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Login bonus'...");
            client.Commands().CreateCommand("login")
                .AddCheck((c, u, ch) => ch.Id == Int64.Parse(config["FGO_general"]))
                .Description("Relay the information of the arrival of the next login bonus.")
                .Do(async cea =>
                {
                    DateTimeWithZone rightNowInJapan = new DateTimeWithZone(DateTime.UtcNow, JpnTimeZone);
                    TimeSpan eta = rightNowInJapan.TimeUntilNextLocalTimeAt(new TimeSpan(hours: 4, minutes: 0, seconds: 0));
                    string h = eta.Hours == 1 ? "hour" : "hours";
                    string m = eta.Minutes == 1 ? "minute" : "minutes";
                    await client.SendMessage(cea.Channel, $"Next login bonus drop **ETA {eta.Hours} {h} and {eta.Minutes} {m}.**");
                });
        }

        public static void RegisterQuartzCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Quartz'...");
            client.Commands().CreateCommand("quartz")
                .AddCheck((c, u, ch) => ch.Id == Int64.Parse(config["FGO_general"]))
                .Description("Relay the prices of different amounts of Saint Quartz.")
                .Do(async cea =>
                    await client.SendMessage(cea.Channel,
                    "Prices for Quartz:\n  1Q: 120 JPY\n  5Q: 480 JPY\n 16Q: 1400 JPY\n 36Q: 2900 JPY\n 65Q: 4800 JPY\n140Q: 9800 JPY"));
        }

        public static void RegisterStatsCommand(this DiscordClient client, IConfiguration config, StatService statService)
        {
            Console.WriteLine("Registering 'Stats'...");
            client.Commands().CreateCommand("stats")
                .AddCheck((c, u, ch) => ch.Id == Int64.Parse(config["FGO_general"]))
                .Parameter("servantname", ParameterType.Multiple)
                .Description($"Relay information on the specified Servant. Alternative names acceptable.")
                .Do(async cea =>
                {
                    var arg = String.Join(" ", cea.Args);
                    if (new[] { "waifu", "mai waifu", "my waifu" }.Contains(arg.ToLowerInvariant()))
                    {
                        await client.SendMessage(cea.Channel, "It has come to my attention that your 'waifu' is equatable to fecal matter.");
                        return;
                    }

                    if (new[] { "enkidu", "arc", "arcueid" }.Contains(arg.ToLowerInvariant()))
                    {
                        await client.SendMessage(cea.Channel, "Never ever.");
                        return;
                    }

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

            Console.WriteLine("Registering 'CE'...");
            client.Commands().CreateCommand("ce")
                .AddCheck((c, u, ch) => ch.Id == Int64.Parse(config["FGO_general"]))
                .Parameter("cename", ParameterType.Multiple)
                .Description($"Relay information on the specified Craft Essence. Alternative names acceptable.")
                .Do(async cea =>
                {
                    var arg = String.Join(" ", cea.Args);

                    var ce = statService.LookupCE(arg);
                    if (ce != null)
                    {
                        await client.SendMessage(cea.Channel, FormatCEProfile(ce));
                    }
                    else
                    {
                        var potentials = FgoHelpers.CEDict.Where(c => c.Alias.Any(a => a.Contains(arg.ToLowerInvariant())));
                        if (potentials.Any())
                        {
                            StringBuilder sb = new StringBuilder();
                            foreach (var p in potentials)
                            {
                                sb.Append($"**{p.CE}** *({p.Alias.First()})*");
                                if (p != potentials.Last())
                                {
                                    sb.AppendLine();
                                }
                            }
                            await client.SendMessage(cea.Channel, $"No such entry found. Did you mean one of the following?\n{sb.ToString()}");
                        }
                        else
                        {
                            await client.SendMessage(cea.Channel, "No such entry found. Please try another name.");
                        }
                    }
                });

            Console.WriteLine("Registering 'All CE'...");
            client.Commands().CreateCommand("allce")
                .AddCheck((c, u, ch) => ch.Id == Int64.Parse(config["FGO_general"]))
                .Parameter("ceeffect", ParameterType.Multiple)
                .Description($"Relay information on CEs having the specified effect.")
                .Do(async cea =>
                {
                    var arg = String.Join(" ", cea.Args);

                    var ces = FgoHelpers.CEProfiles.Where(c => c.Effect.ToLowerInvariant().Contains(arg));
                    if (ces.Count() > 0)
                    {
                        string matches = String.Empty;
                        foreach (var ce in ces)
                        {
                            matches += $"**{ce.Name}** - {ce.Effect}\n";
                        }
                        await client.SendMessage(cea.Channel, matches);
                    }
                    else
                    {
                        await client.SendMessage(cea.Channel, "No such CEs found. Please try another term.");
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
                        case "events":
                            await statService.UpdateEventListsAsync();
                            await client.SendMessage(cea.Channel, "Updated events lookup.");
                            break;
                        case "fcs":
                            FriendCodes.ReadFriendData(config["FriendcodePath"]);
                            await client.SendMessage(cea.Channel, "Updated friendcodes");
                            break;
                        default:
                            statService.ReadAliasList();
                            FriendCodes.ReadFriendData(config["FriendcodePath"]);
                            await statService.UpdateProfileListsAsync();
                            await statService.UpdateEventListsAsync();
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
                        using (TextWriter tw = new StreamWriter(Path.Combine(config["AliasPath"], "servants.json")))
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

            Console.WriteLine("Registering 'CE alias'...");
            client.Commands().CreateCommand("cealias")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && ch.Id == Int64.Parse(config["FGO_general"]))
                .Hide()
                .Parameter("ce", ParameterType.Required)
                .Parameter("alias", ParameterType.Required)
                .Do(async cea =>
                {
                    var newAlias = FgoHelpers.CEDict.SingleOrDefault(p => p.CE == cea.Args[0]);
                    if (newAlias != null)
                    {
                        newAlias.Alias.Add(cea.Args[1]);
                        using (TextWriter tw = new StreamWriter(Path.Combine(config["AliasPath"], "ces.json")))
                        {
                            tw.Write(JsonConvert.SerializeObject(FgoHelpers.CEDict, Formatting.Indented));
                        }
                        await client.SendMessage(cea.Channel, $"Added alias `{cea.Args[1]}` for `{newAlias.CE}`.");
                    }
                    else
                    {
                        await client.SendMessage(cea.Channel, "Could not find name to add alias for.");
                    }
                });

            Console.WriteLine("Registering 'Curve'...");
            client.Commands().CreateCommand("curve")
                .AddCheck((c, u, ch) => ch.Id == Int64.Parse(config["FGO_general"]))
                .Hide()
                .Do(async cea =>
                    await client.SendMessage(cea.Channel,
                        String.Concat(
                            "From master KyteM: `Linear curves scale as you'd expect.\n",
                            "Reverse S means their stats will grow fast, slow the fuck down as they reach the midpoint (with zero or near-zero improvements at that midpoint), then return to their previous growth speed.\n",
                            "S means the opposite. These guys get super little stats at the beginning and end, but are quite fast in the middle (Gonna guesstimate... 35 - 55 in the case of a 5 *).\n",
                            "Semi(reverse) S is like (reverse)S, except not quite as bad in the slow periods and not quite as good in the fast periods.If you graph it it'll go right between linear and non-semi.`")));
        }

        private static string FormatCEProfile(CEProfile ce)
        {
            StringBuilder sb = new StringBuilder()
                .AppendLine($"**Collection ID:** {ce.Id}")
                .AppendLine($"**Rarity:** {ce.Rarity}")
                .AppendLine($"**CE:** {ce.Name}")
                .AppendLine($"**Cost:** {ce.Cost}")
                .AppendLine($"**ATK:** {ce.Atk}")
                .AppendLine($"**HP:** {ce.HP}")
                .AppendLine($"**Effect:** {ce.Effect}")
                .AppendLine($"**Max ATK:** {ce.AtkMax}")
                .AppendLine($"**Max HP:** {ce.HPMax}")
                .AppendLine($"**Max Effect:** {ce.EffectMax}")
                .AppendLine($"{ce.Image}");
            return sb.ToString();
        }

        internal static string FormatServantProfile(ServantProfile profile)
        {
            StringBuilder sb = new StringBuilder()
                .AppendLine($"**Servant:** {profile.Name}")
                .AppendLine($"**Class:** {profile.Class}")
                .AppendLine($"**Rarity:** {profile.Rarity}")
                .AppendLine($"**Collection ID:** {profile.Id}")
                .AppendLine($"**Card pool:** {profile.CardPool}")
                .AppendLine($"**Max ATK:** {profile.Atk}")
                .AppendLine($"**Max HP:** {profile.HP}")
                .AppendLine($"**Starweight:** {profile.Starweight}")
                .AppendLine($"**Growth type:** {profile.GrowthCurve} (Use `.curve` for explanation)")
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

        public TimeSpan TimeUntilNextLocalTimeAt(TimeSpan targetTimeOfDay) =>
            (LocalTime.TimeOfDay > targetTimeOfDay) ?
            TimeSpan.FromDays(1) - (LocalTime.TimeOfDay - targetTimeOfDay) :
            targetTimeOfDay - LocalTime.TimeOfDay;

        public DateTime UniversalTime => utcDateTime;

        public TimeZoneInfo TimeZone => timeZone;

        public DateTime LocalTime => TimeZoneInfo.ConvertTime(utcDateTime, timeZone);
    }
}
