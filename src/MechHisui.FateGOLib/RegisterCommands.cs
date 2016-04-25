using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Commands;
using JiiLib;
using JiiLib.Net;
using Newtonsoft.Json;
using MechHisui.FateGOLib;

namespace MechHisui.Commands
{
    public static class FgoExtensions
    {
        private static StatService statService;

        public static void RegisterAPCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'AP'...");
            client.GetService<CommandService>().CreateCommand("ap")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"]))
                .Parameter("current amount", ParameterType.Optional)
                .Parameter("time left", ParameterType.Optional)
                .Description("Track your current AP.")
                .Do(async cea =>
                {
                    FgoHelpers.UsersAP.RemoveAll(ap => ap.CurrentAP >= 140);
                    if (cea.Args.All(s => s == String.Empty))
                    {
                        var userap = FgoHelpers.UsersAP.SingleOrDefault(u => u.UserID == cea.User.Id);
                        if (userap != null)
                        {
                            await cea.Channel.SendMessage($"{cea.User.Name} currently has {userap.CurrentAP} AP.");
                        }
                        else
                        {
                            await cea.Channel.SendMessage($"Currently not tracking {cea.User.Name}'s AP.");
                        }
                        return;
                    }

                    int startAmount;
                    TimeSpan startTime;
                    if (Int32.TryParse(cea.Args[0], out startAmount) && TimeSpan.TryParseExact(cea.Args[1], @"m\:%s", CultureInfo.InvariantCulture, out startTime))
                    {
                        var tmp = FgoHelpers.UsersAP.SingleOrDefault(ap => ap.UserID == cea.User.Id);
                        if (tmp != null)
                        {
                            FgoHelpers.UsersAP.Remove(tmp);
                        }

                        FgoHelpers.UsersAP.Add(new UserAP
                        {
                            UserID = cea.User.Id,
                            StartAP = startAmount,
                            StartTimeLeft = startTime
                        });

                        await cea.Channel.SendMessage($"Now tracking AP for `{cea.User.Name}`.");
                    }
                    else
                    {
                        await cea.Channel.SendMessage("One or both arguments could not be parsed correctly.");
                    }
                });
        }

        public static void RegisterDailyCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Daily'...");
            client.GetService<CommandService>().CreateCommand("daily")
                .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(config["FGO_server"]))
                .Parameter("day", ParameterType.Optional)
                .Description("Relay the information of daily quests for the specified day. Default to current day.")
                .Do(async cea =>
                {
                    DayOfWeek day;
                    ServantClass serv;
                    DailyInfo info;

                    var todayInJapan = new DateTimeWithZone(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time"));
                    var arg = cea.Args[0];

                    if (String.IsNullOrWhiteSpace(arg) || arg == "today")
                    {
                        day = todayInJapan.LocalTime.DayOfWeek;
                    }
                    else if (arg == "change")
                    {
                        var eta = todayInJapan.TimeUntilNextLocalTimeAt(new TimeSpan(hours: 0, minutes: 0, seconds: 0));
                        string h = eta.Hours == 1 ? "hour" : "hours";
                        string m = eta.Minutes == 1 ? "minute" : "minutes";
                        await cea.Channel.SendMessage($"Daily quests changing **ETA: {eta.Hours} {h} and {eta.Minutes} {m}.**");
                        return;
                    }
                    else if (arg == "tomorrow")
                    {
                        day = todayInJapan.LocalTime.AddDays(1).DayOfWeek;
                    }
                    else if (Enum.TryParse(arg, ignoreCase: true, result: out serv))
                    {
                        day = DailyInfo.DailyQuests.SingleOrDefault(d => d.Value.Materials == serv).Key;
                    }
                    else if (!Enum.TryParse(arg, ignoreCase: true, result: out day))
                    {
                        await cea.Channel.SendMessage("Could not convert argument to a day of the week or Servant class. Please try again.");
                        return;
                    }

                    if (DailyInfo.DailyQuests.TryGetValue(day, out info))
                    {
                        bool isToday = (day == todayInJapan.LocalTime.DayOfWeek);
                        bool isTomorrow = (day == todayInJapan.LocalTime.AddDays(1).DayOfWeek);
                        string whatDay = isToday ? "Today" : (isTomorrow ? "Tomorrow" : day.ToString());
                        if (day != DayOfWeek.Sunday)
                        {
                            await cea.Channel.SendMessage($"{whatDay}'s quests:\n\tAscension Materials: **{info.Materials.ToString()}**\n\tExperience: **{info.Exp1.ToString()}**, **{info.Exp2.ToString()}**, and **{ServantClass.Berserker.ToString()}**");
                        }
                        else
                        {
                            await cea.Channel.SendMessage($"{whatDay}'s quests:\n\tAscension Materials: **{info.Materials.ToString()}**\n\tExperience: **Any Class**\n\tAnd also **QP**");
                        }
                    }
                });
        }

        public static void RegisterEventCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Event'...");
            client.GetService<CommandService>().CreateCommand("event")
                .Alias("events")
                .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(config["FGO_server"]))
                .Description("Relay information on current or upcoming events.")
                .Do(async cea =>
                {
                    var sb = new StringBuilder();
                    var utcNow = DateTime.UtcNow;
                    var currentEvents = FgoHelpers.EventList.Where(e => utcNow > e.StartTime && utcNow < e.EndTime);
                    if (currentEvents.Any())
                    {
                        sb.Append("**Current Event(s):** ");
                        foreach (var ev in currentEvents)
                        {
                            if (ev.EndTime.HasValue)
                            {
                                TimeSpan doneAt = ev.EndTime.Value - utcNow;
                                string d = doneAt.Days == 1 ? "day" : "days";
                                string h = doneAt.Hours == 1 ? "hour" : "hours";
                                string m = doneAt.Minutes == 1 ? "minute" : "minutes";
                                if (doneAt < TimeSpan.FromDays(1))
                                {
                                    sb.AppendLine($"{ev.EventName} for {doneAt.Hours} {h} and {doneAt.Minutes} {m}.");
                                }
                                else
                                {
                                    sb.AppendLine($"{ev.EventName} for {doneAt.Days} {d} and {doneAt.Hours} {h}.");
                                }
                            }
                            else
                            {
                                sb.AppendLine($"{ev.EventName} for unknown time.");
                            }

                            if (!String.IsNullOrEmpty(ev.EventGacha))
                            {
                                sb.AppendLine($"\t**Event gacha rate up on:** {ev.EventGacha}.");
                            }
                            else
                            {
                                sb.AppendLine("\tNo event gacha for this event.");
                            }

                            //if (!String.IsNullOrEmpty(ev.InfoLink))
                            //{
                            //    sb.AppendLine($"\t{ev.InfoLink}");
                            //}
                        }
                    }
                    else
                    {
                        sb.AppendLine("No events currently going on.");
                    }

                    var nextEvent = FgoHelpers.EventList.FirstOrDefault(e => e.StartTime > utcNow) ?? FgoHelpers.EventList.FirstOrDefault(e => !e.StartTime.HasValue);
                    if (nextEvent != null)
                    {
                        if (nextEvent.StartTime.HasValue)
                        {
                            TimeSpan eta = nextEvent.StartTime.Value - utcNow;
                            string d = eta.Days == 1 ? "day" : "days";
                            string h = eta.Hours == 1 ? "hour" : "hours";
                            string m = eta.Minutes == 1 ? "minute" : "minutes";
                            if (eta < TimeSpan.FromDays(1))
                            {
                                sb.AppendLine($"**Next Event:** {nextEvent.EventName}, planned to start in {eta.Hours} {h} and {eta.Minutes} {m}.");
                            }
                            else
                            {
                                sb.AppendLine($"**Next Event:** {nextEvent.EventName}, planned to start in {eta.Days} {d} and {eta.Hours} {h}.");
                            }
                        }
                        else
                        {
                            sb.AppendLine($"**Next Event:** {nextEvent.EventName}, planned to start at an unknown time.");
                        }

                        if (!String.IsNullOrEmpty(nextEvent.InfoLink))
                        {
                            sb.AppendLine(nextEvent.InfoLink);
                        }
                    }
                    else
                    {
                        sb.AppendLine("No known upcoming events.");
                    }
                    sb.Append("KanColle Collab never ever");
                    await cea.Channel.SendMessage(sb.ToString());
                });
        }

        public static void RegisterFriendsCommand(this DiscordClient client, IConfiguration config)
        {
            //statService = statService ?? InitStatService(config);
            Console.WriteLine("Registering 'Friends'...");
            FriendCodes.ReadFriendData(config["FriendcodePath"]);
            client.GetService<CommandService>().CreateCommand("addfc")
               .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_playground"]))
               .Parameter("code", ParameterType.Required)
               .Parameter("class", ParameterType.Required)
               .Parameter("servant", ParameterType.Optional)
               .Description("Add your friendcode to the list. Enter your code with quotes as `\"XXX XXX XXX\"`. You may optionally add your support Servant as well. If you do, enclose that in `\"\"`s as well.")
               .Do(async cea =>
               {
                   if (FriendCodes.friendData.Any(fc => fc.User == cea.User.Name
                           && fc.Class.Equals(cea.Args[1], StringComparison.OrdinalIgnoreCase)))
                   {
                       await cea.Channel.SendMessage($"Already in the Friendcode list. Please use `.updatefc` to update your description.");
                       return;
                   }

                   SupportClass support;
                   if (!Enum.TryParse(cea.GetArg("class"), true, out support))
                   {
                       await cea.Channel.SendMessage("Could not parse `class` parameter as valid suport slot.");
                       return;
                   }

                   if (Regex.Match(cea.Args[0], @"[0-9][0-9][0-9] [0-9][0-9][0-9] [0-9][0-9][0-9]").Success)
                   {
                       var friend = new FriendData
                       {
                           Id = FriendCodes.friendData.Count + 1,
                           User = cea.User.Name,
                           FriendCode = cea.Args[0],
                           Class = support.ToString(),
                           Servant = (cea.Args.Length > 2) ? cea.Args[2] : String.Empty
                       };
                       FriendCodes.friendData.Add(friend);
                       FriendCodes.WriteFriendData(config["FriendcodePath"]);
                       await cea.Channel.SendMessage($"Added {support.ToString()} support for `{friend.User}`.");
                   }
                   else
                   {
                       await cea.Channel.SendMessage($"Incorrect friendcode format specified.");
                   }
               });

            client.GetService<CommandService>().CreateCommand("listfcs")
               .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_playground"]))
               .Parameter("filter", ParameterType.Required)
               .Description("Display known friendcodes.")
               .Do(async cea =>
               {
                   var data = Enumerable.Empty<FriendData>();
                   SupportClass support;
                   if (Enum.TryParse(cea.Args[0], true, out support))
                   {
                       data = FriendCodes.friendData.Where(f => f.Class == support.ToString());
                   }
                   else
                   {
                       await cea.Channel.SendMessage("Could not parse parameter as a valid filter.");
                       return;
                   }

                   var sb = new StringBuilder("```\n");
                   int longestName = data.OrderByDescending(f => f.User.Length).First().User.Length;
                   foreach (var friend in data.OrderBy(f => f.Id))
                   {
                       var spaces = new string(' ', (longestName - friend.User.Length) + 1);
                       sb.Append($"{friend.User}:{spaces}{friend.FriendCode}");
                       sb.AppendLine((!String.IsNullOrEmpty(friend.Servant)) ? $" - {friend.Servant}" : String.Empty);
                       if (sb.Length > 1700)
                       {
                           sb.Append("\n```");
                           await cea.Channel.SendMessage(sb.ToString());
                           sb = sb.Clear().AppendLine("```");
                       }
                   }
                   sb.Append("\n```");
                   await cea.Channel.SendMessage(sb.ToString());
               });

            client.GetService<CommandService>().CreateCommand("updatefc")
               .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"]))
               .Parameter("class", ParameterType.Required)
               .Parameter("newServant", ParameterType.Required)
               .Description("Update the Support Servant displayed in your friendcode listing.")
               .Do(async cea =>
               {
                   SupportClass support;
                   if (!Enum.TryParse(cea.GetArg("class"), true, out support))
                   {
                       await cea.Channel.SendMessage("Could not parse `class` parameter as valid support slot.");
                       return;
                   }

                   Func<FriendData, bool> pred = c => c.User == cea.User.Name && c.Class == support.ToString();
                   if (FriendCodes.friendData.Any(pred))
                   {
                       var temp = FriendCodes.friendData.Single(pred);
                       FriendCodes.friendData.Remove(FriendCodes.friendData.Single(pred));
                       temp.Class = support.ToString();
                       temp.Servant = cea.GetArg("newServant");
                       FriendCodes.friendData.Add(temp);
                       FriendCodes.WriteFriendData(config["FriendcodePath"]);
                       await cea.Channel.SendMessage($"Updated `{temp.User}`'s {support.ToString()} Suppport Servant to be `{temp.Servant}`.");
                   }
                   else
                   {
                       await cea.Channel.SendMessage("Profile not found. Please add your profile using `.addfc`.");
                   }
               });
        }

        public static void RegisterLoginBonusCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Login bonus'...");
            client.GetService<CommandService>().CreateCommand("login")
                .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(config["FGO_server"]))
                .Description("Relay the information of the arrival of the next login bonus.")
                .Do(async cea =>
                {
                    var rightNowInJapan = new DateTimeWithZone(DateTime.UtcNow, FgoHelpers.JpnTimeZone);
                    TimeSpan eta = rightNowInJapan.TimeUntilNextLocalTimeAt(new TimeSpan(hours: 4, minutes: 0, seconds: 0));
                    string h = eta.Hours == 1 ? "hour" : "hours";
                    string m = eta.Minutes == 1 ? "minute" : "minutes";
                    await cea.Channel.SendMessage($"Next login bonus drop **ETA {eta.Hours} {h} and {eta.Minutes} {m}.**");
                });

            LoginBonusTimer = new Timer(async cb =>
            {
                Console.WriteLine("Announcing login bonuses.");
                await ((DiscordClient)cb).GetChannel(UInt64.Parse(config["FGO_general"]))
                    .SendMessage("Login bonuses have been distributed.");
            },
            client,
            new DateTimeWithZone(DateTime.UtcNow, FgoHelpers.JpnTimeZone)
                .TimeUntilNextLocalTimeAt(new TimeSpan(hours: 3, minutes: 59, seconds: 57)),
            TimeSpan.FromDays(1));
        }

        public static void RegisterQuartzCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Quartz'...");
            client.GetService<CommandService>().CreateCommand("quartz")
                .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(config["FGO_server"]))
                .Description("Relay the prices of different amounts of Saint Quartz.")
                .Do(async cea =>
                    await cea.Channel.SendMessage(
@"Prices for Quartz:
```
  1Q:  120 JPY
  5Q:  480 JPY
 16Q: 1400 JPY
 36Q: 2900 JPY
 65Q: 4800 JPY
140Q: 9800 JPY
```"));
        }

        public static void RegisterStatsCommands(this DiscordClient client, IConfiguration config)
        {
            statService = statService ?? InitStatService(config);

            Console.WriteLine("Registering 'Stats'...");
            client.GetService<CommandService>().CreateCommand("servant")
                .Alias("stat")
                .Alias("stats")
                .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(config["FGO_server"]))
                .Parameter("name", ParameterType.Unparsed)
                .Description($"Relay information on the specified Servant. Alternative names acceptable.")
                .Do(async cea =>
                {
                    if (cea.Args[0].ContainsIgnoreCase("waifu"))
                    {
                        await cea.Channel.SendMessage("It has come to my attention that your 'waifu' is equatable to fecal matter.");
                        return;
                    }

                    if (new[] { "enkidu", "arc", "arcueid" }.ContainsIgnoreCase(cea.Args[0]))
                    {
                        await cea.Channel.SendMessage("Never ever.");
                        return;
                    }

                    ServantProfile profile;
                    int id;
                    if (Int32.TryParse(cea.Args[0], out id))
                    {
                        profile = FgoHelpers.ServantProfiles.SingleOrDefault(p => p.Id == id) ??
                            FgoHelpers.FakeServantProfiles.SingleOrDefault(p => p.Id == id);
                    }
                    else
                    {
                        profile = statService.LookupStats(cea.Args[0]);
                    }

                    if (profile != null)
                    {
                        await cea.Channel.SendMessage(FormatServantProfile(profile));
                    }
                    else
                    {
                        var name = statService.LookupServantName(cea.Args[0]);
                        if (name != null)
                        {
                            await cea.Channel.SendMessage($"**Servant:** {name}\nMore information TBA.");
                        }
                        else
                        {
                            await cea.Channel.SendMessage("No such entry found. Please try another name.");
                        }
                    }
                });

            Console.WriteLine("Registering 'CE'...");
            client.GetService<CommandService>().CreateCommand("ce")
                .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(config["FGO_server"]))
                .Parameter("cename", ParameterType.Unparsed)
                .Description($"Relay information on the specified Craft Essence. Alternative names acceptable.")
                .Do(async cea =>
                {
                    CEProfile ce;
                    int id;
                    if (Int32.TryParse(cea.Args[0], out id) && id <= FgoHelpers.CEProfiles.Max(p => p.Id))
                    {
                        ce = FgoHelpers.CEProfiles.SingleOrDefault(p => p.Id == id);
                    }
                    else
                    {
                        ce = statService.LookupCE(cea.Args[0]);
                    }

                    if (ce != null)
                    {
                        await cea.Channel.SendMessage(FormatCEProfile(ce));
                    }
                    else
                    {
                        var potentials = FgoHelpers.CEDict.Where(c => c.Alias.Any(a => a.ContainsIgnoreCase(cea.Args[0])) || c.CE.ContainsIgnoreCase(cea.Args[0]));
                        if (potentials.Any())
                        {
                            if (potentials.Count() > 1)
                            {
                                string res = String.Join("\n", potentials.Select(p => $"**{p.CE}** *({String.Join(", ", p.Alias)})*"));
                                await cea.Channel.SendMessage($"Entry ambiguous. Did you mean one of the following?\n{res}");
                            }
                            else
                            {
                                await cea.Channel.SendMessage($"**CE:** {potentials.First().CE}\nMore information TBA.");
                            }
                        }
                        else
                        {
                            await cea.Channel.SendMessage("No such entry found. Please try another name.");
                        }
                    }
                });

            //Console.WriteLine("Registering 'All CE'...");
            //client.GetService<CommandService>().CreateCommand("allce")
            //    .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(config["FGO_server"]))
            //    .Parameter("ceeffect", ParameterType.Unparsed)
            //    .Description($"Relay information on CEs having the specified effect.")
            //    .Do(async cea =>
            //    {
            //        Console.WriteLine($"{DateTime.Now}: Command `allce` invoked.");
            //        var arg = cea.Args[0];
            //        Console.WriteLine($"Argument: {arg}.");
            //        var ces = FgoHelpers.CEProfiles.Where(c => c.Effect.ContainsIgnoreCase(arg));
            //        Console.WriteLine($"Amount of results: {ces.Count()}.");
            //        if (ces.Count() > 0)
            //        {
            //            Console.WriteLine("Making StringBuilder.");
            //            var sb = new StringBuilder($"`{arg}`:\n");
            //            foreach (var c in ces)
            //            {
            //                Console.WriteLine("Appending to StringBuilder.");
            //                sb.AppendLine($"**{c.Name}** - {c.Effect}");
            //                if (sb.Length > 1700)
            //                {
            //                    Console.WriteLine("Sending and Clearing.");
            //                    await cea.Channel.SendMessage(sb.ToString());
            //                    sb = sb.Clear();
            //                }
            //            }
            //            Console.WriteLine("Sending.");
            //            await cea.Channel.SendMessage(sb.ToString());
            //        }
            //        else
            //        {
            //            await cea.Channel.SendMessage("No such CEs found. Please try another term.");
            //        }
            //    });

            Console.WriteLine("Registering 'Update'...");
            client.GetService<CommandService>().CreateCommand("update")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]))
                .Parameter("item", ParameterType.Optional)
                .Hide()
                .Do(async cea =>
                {
                    await cea.Channel.SendIsTyping();
                    switch (cea.Args[0])
                    {
                        case "alias":
                            statService.ReadAliasList();
                            await cea.Channel.SendMessage("Updated alias lookups.");
                            break;
                        case "profiles":
                            await statService.UpdateProfileListsAsync();
                            await cea.Channel.SendMessage("Updated profile lookups.");
                            break;
                        case "ces":
                            await statService.UpdateCEListAsync();
                            await cea.Channel.SendMessage("Updated CE lookup.");
                            break;
                        case "events":
                            await statService.UpdateEventListAsync();
                            await cea.Channel.SendMessage("Updated events lookup.");
                            break;
                        case "fcs":
                            FriendCodes.ReadFriendData(config["FriendcodePath"]);
                            await cea.Channel.SendMessage("Updated friendcodes");
                            break;
                        case "mystic":
                            await statService.UpdateMysticCodesListAsync();
                            await cea.Channel.SendMessage("Updated Mystic Codes lookup.");
                            break;
                        case "drops":
                            await statService.UpdateDropsListAsync();
                            await cea.Channel.SendMessage("Updated Item Drops lookup.");
                            break;
                        default:
                            statService.ReadAliasList();
                            FriendCodes.ReadFriendData(config["FriendcodePath"]);
                            await statService.UpdateProfileListsAsync();
                            await statService.UpdateCEListAsync();
                            await statService.UpdateEventListAsync();
                            await statService.UpdateMysticCodesListAsync();
                            await statService.UpdateDropsListAsync();
                            await cea.Channel.SendMessage("Updated all lookups.");
                            break;
                    }
                });

            Console.WriteLine("Registering 'Add alias'...");
            client.GetService<CommandService>().CreateCommand("addalias")
                .AddCheck((c, u, ch) => u.Roles.Select(r => r.Id).Contains(UInt64.Parse(config["FGO_Admins"])) && ch.Server.Id == UInt64.Parse(config["FGO_server"]))
                .Hide()
                .Parameter("servant", ParameterType.Required)
                .Parameter("alias", ParameterType.Required)
                .Do(async cea =>
                {
                    ServantAlias newAlias = FgoHelpers.ServantDict.SingleOrDefault(p => p.Servant == cea.Args[0]);
                    var arg = cea.Args[1].ToLowerInvariant();
                    var test = FgoHelpers.ServantDict.Where(a => a.Alias.Contains(arg)).FirstOrDefault();
                    if (test != null)
                    {
                        await cea.Channel.SendMessage($"Alias `{arg}` already exists for Servant `{test.Servant}`.");
                        return;
                    }
                    else if (newAlias != null)
                    {
                        newAlias.Alias.Add(arg);
                    }
                    else
                    {
                        ServantProfile profile = FgoHelpers.ServantProfiles.SingleOrDefault(s => s.Name == cea.Args[0]);
                        if (profile != null)
                        {
                            newAlias = new ServantAlias
                            {
                                Alias = new List<string> { arg },
                                Servant = profile.Name
                            };
                        }
                        else
                        {
                            await cea.Channel.SendMessage("Could not find name to add alias for.");
                            return;
                        }
                    }

                    File.WriteAllText(Path.Combine(config["AliasPath"], "servants.json"), JsonConvert.SerializeObject(FgoHelpers.ServantDict, Formatting.Indented));
                    await cea.Channel.SendMessage($"Added alias `{arg}` for `{newAlias.Servant}`.");
                });

            Console.WriteLine("Registering 'CE alias'...");
            client.GetService<CommandService>().CreateCommand("cealias")
                .AddCheck((c, u, ch) => u.Roles.Select(r => r.Id).Contains(UInt64.Parse(config["FGO_Admins"])) && ch.Server.Id == UInt64.Parse(config["FGO_server"]))
                .Hide()
                .Parameter("ce", ParameterType.Required)
                .Parameter("alias", ParameterType.Required)
                .Do(async cea =>
                {
                    CEAlias newAlias = FgoHelpers.CEDict.SingleOrDefault(p => p.CE == cea.Args[0]);
                    var arg = cea.Args[1].ToLowerInvariant();
                    var test = FgoHelpers.CEDict.Where(a => a.Alias.Contains(arg)).FirstOrDefault();
                    if (test != null)
                    {
                        await cea.Channel.SendMessage($"Alias `{arg}` already exists for CE `{test.CE}`.");
                        return;
                    }
                    else if (newAlias != null)
                    {
                        newAlias.Alias.Add(arg);
                    }
                    else
                    {
                        CEProfile ce = FgoHelpers.CEProfiles.SingleOrDefault(s => s.Name == cea.Args[0]);
                        if (ce != null)
                        {
                            newAlias = new CEAlias
                            {
                                Alias = new List<string> { arg },
                                CE = ce.Name
                            };
                        }
                        else
                        {
                            await cea.Channel.SendMessage("Could not find name to add alias for.");
                            return;
                        }
                    }

                    File.WriteAllText(Path.Combine(config["AliasPath"], "ces.json"), JsonConvert.SerializeObject(FgoHelpers.CEDict, Formatting.Indented));
                    await cea.Channel.SendMessage($"Added alias `{arg}` for `{newAlias.CE}`.");
                });

            Console.WriteLine("Registering 'Curve'...");
            client.GetService<CommandService>().CreateCommand("curve")
                .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(config["FGO_server"]))
                .Hide()
                .Do(async cea =>
                    await cea.Channel.SendMessage(
                        String.Concat(
                            "From master KyteM: `Linear curves scale as you'd expect.\n",
                            "Reverse S means their stats will grow fast, slow the fuck down as they reach the midpoint (with zero or near-zero improvements at that midpoint), then return to their previous growth speed.\n",
                            "S means the opposite. These guys get super little stats at the beginning and end, but are quite fast in the middle (Gonna guesstimate... 35 - 55 in the case of a 5 *).\n",
                            "Semi(reverse) S is like (reverse)S, except not quite as bad in the slow periods and not quite as good in the fast periods.If you graph it it'll go right between linear and non-semi.`")));

            Console.WriteLine("Registering 'Mystic Codes'...");
            client.GetService<CommandService>().CreateCommand("mystic")
                .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(config["FGO_server"]))
                .Description("Relay information on available Mystic Codes.")
                .Parameter("code", ParameterType.Unparsed)
                .Do(async cea =>
                {
                    string arg = String.Join(" ", cea.Args);

                    if (arg.ToLowerInvariant() == "chaldea")
                    {
                        await cea.Channel.SendMessage("Search term ambiguous. Please be more specific.");
                        return;
                    }

                    MysticCode code = statService.LookupMystic(arg);
                    if (code == null)
                    {
                        await cea.Channel.SendMessage("Specified Mystic Code not found. Please use `.listmystic` for the list of available Mystic Codes.");
                    }
                    else
                    {
                        await cea.Channel.SendMessage(FormatMysticCodeProfile(code));
                    }
                });

            client.GetService<CommandService>().CreateCommand("listmystic")
                .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(config["FGO_server"]))
                .Description("Relay the names of available Mystic Codes.")
                .Do(async cea =>
                {
                    var sb = new StringBuilder("**Available Mystic Codes:**\n");
                    foreach (var code in FgoHelpers.MysticCodeList)
                    {
                        sb.AppendLine(code.Code);
                    }
                    await cea.Channel.SendMessage(sb.ToString());
                });

            Console.WriteLine("Registering 'Mystic alias'...");
            client.GetService<CommandService>().CreateCommand("mysticalias")
                .AddCheck((c, u, ch) => u.Roles.Select(r => r.Id).Contains(UInt64.Parse(config["FGO_Admins"])) && ch.Server.Id == UInt64.Parse(config["FGO_server"]))
                .Hide()
                .Parameter("mystic", ParameterType.Required)
                .Parameter("alias", ParameterType.Required)
                .Do(async cea =>
                {
                    var newAlias = FgoHelpers.MysticCodeDict.SingleOrDefault(p => p.Code == cea.Args[0]);
                    var arg = cea.Args[1].ToLowerInvariant();
                    var test = FgoHelpers.MysticCodeDict.Where(a => a.Alias.Contains(arg)).FirstOrDefault();
                    if (test != null)
                    {
                        await cea.Channel.SendMessage($"Alias `{arg}` already exists for CE `{test.Code}`.");
                        return;
                    }
                    else
                    if (newAlias != null)
                    {
                        newAlias.Alias.Add(arg);
                    }
                    else
                    {
                        await cea.Channel.SendMessage("Could not find Mystic Code to add alias for.");
                        return;
                    }

                    File.WriteAllText(Path.Combine(config["AliasPath"], "mystic.json"), JsonConvert.SerializeObject(FgoHelpers.CEDict, Formatting.Indented));
                    await cea.Channel.SendMessage($"Added alias `{arg}` for `{newAlias.Code}`.");
                });

            Console.WriteLine("Registering 'Drops'...");
            client.GetService<CommandService>().CreateCommand("drops")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_playground"]))
                .Description("Relay information about item drop locations.")
                .Parameter("item", ParameterType.Unparsed)
                .Do(async cea =>
                {
                    var arg = String.Join(" ", cea.Args);
                    if (String.IsNullOrWhiteSpace(arg))
                    {
                        await cea.Channel.SendMessage("Provide an item to find among drops.");
                        return;
                    }

                    var potentials = FgoHelpers.ItemDropsList.Where(d => d.ItemDrops.ContainsIgnoreCase(arg));
                    if (potentials.Any())
                    {
                        string result = String.Join("\n", potentials.Select(p => $"**{p.Map} - {p.NodeJP} ({p.NodeEN}):** {p.ItemDrops}"));
                        if (result.Length > 1900)
                        {
                            for (int i = 0; i < result.Length; i += 1750)
                            {
                                if (i == 0)
                                {
                                    await cea.Channel.SendMessage($"Found in the following {potentials.Count()} locations:\n{result.Substring(i, i + 1750)}...");
                                }
                                else if (i + 1750 > result.Length)
                                {
                                    await cea.Channel.SendMessage($"...{result.Substring(i)}");
                                }
                                else
                                {
                                    await cea.Channel.SendMessage($"...{result.Substring(i, i + 1750)}");
                                }
                            }
                        }
                        else
                        {
                            await cea.Channel.SendMessage($"Found in the following {potentials.Count()} locations:\n{result}");
                        }

                    }
                    else
                    {
                        await cea.Channel.SendMessage("Could not find specified item among location drops.");
                    }
                });

            Console.WriteLine("Registering 'HGW'...");
            FgoHelpers.InitRandomHgw(config);
            client.GetService<CommandService>().CreateCommand("hgw")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_playground"]))
                .Description("Set up a random Holy Grail War. Discuss.")
                .Do(async cea =>
                {
                    var rng = new Random();
                    var masters = new List<string>();
                    for (int i = 0; i < 7; i++)
                    {
                        FgoHelpers.Masters = (List<string>)FgoHelpers.Masters.Shuffle();
                        string temp;
                        do temp = FgoHelpers.Masters.ElementAt(rng.Next(maxValue: FgoHelpers.Masters.Count));
                        while (masters.Contains(temp));
                        masters.Add(temp);
                    }

                    Func<ServantProfile, bool> pred = p =>
                        p.Class == ServantClass.Saber.ToString() ||
                        p.Class == ServantClass.Archer.ToString() ||
                        p.Class == ServantClass.Lancer.ToString() ||
                        p.Class == ServantClass.Rider.ToString() ||
                        p.Class == ServantClass.Caster.ToString() ||
                        p.Class == ServantClass.Assassin.ToString() ||
                        p.Class == ServantClass.Berserker.ToString();
                    var templist = FgoHelpers.ServantProfiles.Concat(FgoHelpers.FakeServantProfiles)
                        .Where(pred)
                        .Select(p => new NameOnlyServant { Class = p.Class, Name = p.Name })
                        .Concat(FgoHelpers.NameOnlyServants);

                    var servants = new List<NameOnlyServant>();
                    for (int i = 0; i < 7; i++)
                    {
                        templist = templist.Shuffle();
                        NameOnlyServant temp;
                        do temp = templist.ElementAt(rng.Next(maxValue: templist.Count()));
                        while (servants.Select(s => s.Class).Contains(temp.Class));
                        servants.Add(temp);
                    }

                    var hgw = new Dictionary<string, string>
                    {
                        { masters.ElementAt(0), servants.Single(p => p.Class == ServantClass.Saber.ToString()).Name },
                        { masters.ElementAt(1), servants.Single(p => p.Class == ServantClass.Archer.ToString()).Name },
                        { masters.ElementAt(2), servants.Single(p => p.Class == ServantClass.Lancer.ToString()).Name },
                        { masters.ElementAt(3), servants.Single(p => p.Class == ServantClass.Rider.ToString()).Name },
                        { masters.ElementAt(4), servants.Single(p => p.Class == ServantClass.Caster.ToString()).Name },
                        { masters.ElementAt(5), servants.Single(p => p.Class == ServantClass.Assassin.ToString()).Name },
                        { masters.ElementAt(6), servants.Single(p => p.Class == ServantClass.Berserker.ToString()).Name }
                    };

                    var sb = new StringBuilder($"**Team Saber:** {hgw.ElementAt(0).Key} + {hgw.ElementAt(0).Value}\n")
                        .AppendLine($"**Team Archer:** {hgw.ElementAt(1).Key} + {hgw.ElementAt(1).Value}")
                        .AppendLine($"**Team Lancer:** {hgw.ElementAt(2).Key} + {hgw.ElementAt(2).Value}")
                        .AppendLine($"**Team Rider:** {hgw.ElementAt(3).Key} + {hgw.ElementAt(3).Value}")
                        .AppendLine($"**Team Caster:** {hgw.ElementAt(4).Key} + {hgw.ElementAt(4).Value}")
                        .AppendLine($"**Team Assassin:** {hgw.ElementAt(5).Key} + {hgw.ElementAt(5).Value}")
                        .AppendLine($"**Team Berserker:** {hgw.ElementAt(6).Key} + {hgw.ElementAt(6).Value}")
                        .Append("Discuss.");

                    await cea.Channel.SendMessage(sb.ToString());
                });

            client.GetService<CommandService>().CreateCommand("addhgw")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && ch.Server.Id == UInt64.Parse(config["FGO_server"]))
                .Hide()
                .Parameter("cat", ParameterType.Required)
                .Parameter("name", ParameterType.Multiple)
                .Do(async cea =>
                {
                    switch (cea.Args[0])
                    {
                        case "servant":
                            var temp = cea.Args[1].Split(' ');
                            var name = String.Join(" ", temp.Skip(1));
                            FgoHelpers.NameOnlyServants.Add(
                                new NameOnlyServant
                                {
                                    Class = temp[0],
                                    Name = name
                                });
                            using (TextWriter tw = new StreamWriter(Path.Combine(config["other"], "nameonlyservants.json")))
                            {
                                tw.Write(JsonConvert.SerializeObject(FgoHelpers.NameOnlyServants, Formatting.Indented));
                            }
                            await cea.Channel.SendMessage($"Added `{name}` as a `{temp[0]}`.");
                            break;
                        case "master":
                            FgoHelpers.Masters.Add(cea.Args[1]);
                            using (TextWriter tw = new StreamWriter(Path.Combine(config["other"], "masters.json")))
                            {
                                tw.Write(JsonConvert.SerializeObject(FgoHelpers.Masters, Formatting.Indented));
                            }
                            await cea.Channel.SendMessage($"Added `{cea.Args[1]}` as a Master.");
                            break;
                        default:
                            await cea.Channel.SendMessage("Unsupported catagory.");
                            break;
                    }
                });

            Console.WriteLine("Registering 'Trait'...");
            client.GetService<CommandService>().CreateCommand("trait")
                .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(config["FGO_server"]))
                .Description("Find Servants by trait.")
                .Parameter("trait", ParameterType.Required)
                .Do(async cea =>
                {
                    string trait = null;
                    var servants = FgoHelpers.ServantProfiles
                        .SelectMany(p => p.Traits.Where(t =>
                        {
                            var r = t.ContainsIgnoreCase(cea.Args[0]);
                            if (r)
                            {
                                trait = t;
                            }
                            return r;
                        })
                        .Select(s => p.Name))
                        .ToList();
                    if (trait == null)
                    {
                        await cea.Channel.SendMessage("Could not find trait.");
                    }
                    else if (servants.Count == 0)
                    {
                        await cea.Channel.SendMessage("No results for that query.");
                    }
                    else
                    {
                        await cea.Channel.SendMessage($"**{trait}:** {String.Join(", ", servants)}.");
                    }
                });

            Console.WriteLine("Registering 'Gacha'...");
            #region vars
            var rolltypes = new[] { "fp1", "fp10", "ticket", "4q", "40q" };
            var fpOnly = new[]
            {
                "Azoth Blade",
                "Book of the False Attendant",
                "Blue Black Keys",
                "Green Black Keys",
                "Red Black Keys",
                "Rin's Pendant",
                "Grimoire",
                "Leyline",
                "Magic Crystal",
                "Dragonkin",
            };
            var premiumPool = FgoHelpers.ServantProfiles
                .Where(p => p.Obtainable)
                .Where(p => p.Rarity >= 3)
                .Concat(FgoHelpers.ServantProfiles
                    .Where(p => p.Rarity >= 3 && p.Rarity <= 4)
                    .RepeatSeq(5))
                .Concat(FgoHelpers.ServantProfiles
                    .Where(p => p.Rarity == 3)
                    .RepeatSeq(5))
                .Select(p => p.Name)
                .Concat(FgoHelpers.CEProfiles
                    .Where(ce => ce.Obtainable)
                    .Where(ce => ce.Rarity >= 3)
                    .Concat(FgoHelpers.CEProfiles
                        .Where(ce => ce.Rarity >= 3 && ce.Rarity <= 4)
                        .RepeatSeq(5))
                    .Concat(FgoHelpers.CEProfiles
                        .Where(ce => ce.Rarity == 3)
                        .RepeatSeq(5))
                    .Select(ce => ce.Name));
            var fpPool = FgoHelpers.ServantProfiles
                .Where(p => p.Obtainable)
                .Where(p => p.Rarity <= 3)
                .Concat(FgoHelpers.ServantProfiles
                    .Where(p => p.Rarity <= 2)
                    .RepeatSeq(5))
                .Concat(FgoHelpers.ServantProfiles
                    .Where(p => p.Rarity == 1)
                    .RepeatSeq(5))
                .Select(p => p.Name)
                .Concat(FgoHelpers.CEProfiles
                    .Where(ce => ce.Obtainable)
                    .Where(ce => ce.Rarity <= 3)
                    .Concat(FgoHelpers.CEProfiles
                        .Where(ce => ce.Rarity <= 2)
                        .RepeatSeq(5))
                    .Concat(FgoHelpers.CEProfiles
                        .Where(ce => ce.Rarity == 1)
                        .RepeatSeq(5))
                    .Select(ce => ce.Name))
                .Concat(fpOnly.RepeatSeq(5));
            #endregion
            client.GetService<CommandService>().CreateCommand("gacha")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_playground"]))
                .Description("Simulate gacha roll (not accurate wrt rarity ratios and rate ups). Accepetable parameters are `fp1`, `fp10`, `ticket`, `4`, and `40`")
                .Parameter("type", ParameterType.Optional)
                .Do(async cea =>
                {
                    await cea.Channel.SendMessage("This command temporarily disabled.");
                    //if (!rolltypes.Contains(cea.Args[0]))
                    //{
                    //    await cea.Channel.SendMessage("Unaccaptable parameter.");
                    //    return;
                    //}

                    //var rng = new Random();
                    //IEnumerable<string> pool = (cea.Args[0] == "fp" || cea.Args[0] == "fp10") ? fpPool : premiumPool;
                    //List<string> picks = new List<string>();

                    //for (int i = 0; i < 28; i++)
                    //{
                    //    pool = pool.Shuffle();
                    //}

                    //if (cea.Args[0] == "fp" || cea.Args[0] == "ticket" || cea.Args[0] == "4")
                    //{
                    //    pool = pool.Shuffle();
                    //    picks.Add(pool.ElementAt(rng.Next(maxValue: pool.Count())));
                    //}
                    //else //10-roll
                    //{
                    //    for (int i = 0; i < 10; i++)
                    //    {
                    //        pool = pool.Shuffle();
                    //        picks.Add(pool.ElementAt(rng.Next(maxValue: pool.Count())));
                    //    }
                    //}

                    //await cea.Channel.SendMessage($"**{cea.User.Name} rolled:** {String.Join(", ", picks)}");
                });

            Console.WriteLine("Registering 'Simulate'...");
            client.GetService<CommandService>().CreateCommand("simdmg")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_playground"]))
                .Description("Roughly approximate an attacks damage (not accounting for NP, Crit, buffs/debuffs).")
                .Parameter("servant", ParameterType.Required)
                .Parameter("enemyClass", ParameterType.Required)
                .Parameter("atk", ParameterType.Required)
                .Parameter("atkCard", ParameterType.Required)
                .Parameter("atkIndex", ParameterType.Required)
                .Do(async cea =>
                {
                    int atk;
                    if (!Int32.TryParse(cea.GetArg("atk"), out atk))
                    {
                        await cea.Channel.SendMessage("Could not parse `atk` parameter as number.");
                        return;
                    }

                    Card atkCard;
                    if (!Enum.TryParse<Card>(cea.GetArg("atkCard"), true, out atkCard))
                    {
                        await cea.Channel.SendMessage("Could not parse `atkCard` parameter as a valid attack type.");
                        return;
                    }

                    int index;
                    if (atkCard != Card.Extra)
                    {
                        if (!Int32.TryParse(cea.GetArg("atkIndex"), out index))
                        {
                            await cea.Channel.SendMessage("Could not parse `atkIndex` parameter as a number.");
                            return;
                        }
                        else if (index < 1 || index > 3)
                        {
                            await cea.Channel.SendMessage("Parameter `atkIndex` not in valid range.");
                            return;
                        }
                    }
                    else
                    {
                        index = 4;
                    }


                    ServantProfile profile;
                    int id;
                    if (Int32.TryParse(cea.GetArg("servant"), out id))
                    {
                        profile = FgoHelpers.ServantProfiles.SingleOrDefault(p => p.Id == id);
                    }
                    else
                    {
                        profile = statService.LookupStats(cea.GetArg("servant"));
                    }

                    if (profile == null)
                    {
                        await cea.Channel.SendMessage("Could not find specified Servant.");
                        return;
                    }
                    else
                    {
                        await cea.Channel.SendMessage($"**Approximate damage dealt:** {SimulateDmg(profile, cea.GetArg("enemyClass"), atk, atkCard, index):N3}");
                        return;
                    }
                });
        }

        private static StatService InitStatService(IConfiguration config)
        {
            Console.WriteLine("Connecting to data service...");
            var apiService = new GoogleScriptApiService(
                Path.Combine(config["Google_Secrets_Path"], "client_secret.json"),
                Path.Combine(config["Google_Secrets_Path"], "scriptcreds"),
                "MechHisui",
                config["Project_Key"],
                "exportSheet",
                new string[]
                {
                    "https://www.googleapis.com/auth/spreadsheets",
                    "https://www.googleapis.com/auth/drive",
                    "https://spreadsheets.google.com/feeds/"
                });

            var statService = new StatService(apiService,
                servantAliasPath: Path.Combine(config["AliasPath"], "servants.json"),
                ceAliasPath: Path.Combine(config["AliasPath"], "ces.json"),
                mysticAliasPath: Path.Combine(config["AliasPath"], "mystics.json"));
            try
            {
                //Using .GetAwaiter().GetResult() here since there is no proper async context that await works
                statService.UpdateProfileListsAsync().GetAwaiter().GetResult();
                statService.UpdateCEListAsync().GetAwaiter().GetResult();
                statService.UpdateEventListAsync().GetAwaiter().GetResult();
                statService.UpdateMysticCodesListAsync().GetAwaiter().GetResult();
                statService.UpdateDropsListAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now}: {ex.Message}");
                Environment.Exit(0);
            }

            return statService;
        }

        public static void RegisterZoukenCommand(this DiscordClient client, IConfiguration config)
        {
            client.GetService<CommandService>().CreateCommand("zouken")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"]))
                .Hide()
                .Do(async cea =>
                {
                    await cea.Channel.SendMessage("Friendly reminder that the Zouken CE doesn't trigger on suicides, so don't even think about pairing it with A-Trash.");
                });
        }

        public static string FormatCEProfile(CEProfile ce)
        {
            return new StringBuilder()
                .AppendLine($"**Collection ID:** {ce.Id}")
                .AppendLine($"**Rarity:** {ce.Rarity}☆")
                .AppendLine($"**CE:** {ce.Name}")
                .AppendLine($"**Cost:** {ce.Cost}")
                .AppendLine($"**ATK:** {ce.Atk}")
                .AppendLine($"**HP:** {ce.HP}")
                .AppendLine($"**Effect:** {ce.Effect}")
                .AppendLine($"**Max ATK:** {ce.AtkMax}")
                .AppendLine($"**Max HP:** {ce.HPMax}")
                .AppendLine($"**Max Effect:** {ce.EffectMax}")
                .Append(ce.Image)
                .ToString();
        }

        public static string FormatServantProfile(ServantProfile profile)
        {
            string aoe = ((profile.NoblePhantasmEffect?.Contains("AoE") == true) && Regex.Match(profile.NoblePhantasmEffect, "([2-9]|10)H").Success) ? " (Hits is per enemy)" : String.Empty;
            int a = 1;
            int p = 1;
            return new StringBuilder()
                .AppendWhen(() => profile.Id == -3, b => b.Append("~~"))
                .AppendLine($"**Collection ID:** {profile.Id}")
                .AppendLine($"**Rarity:** {profile.Rarity}☆")
                .AppendLine($"**Class:** {profile.Class}")
                .AppendLine($"**Servant:** {profile.Name}")
                .AppendLine($"**Gender:** {profile.Gender}")
                .AppendLine($"**Card pool:** {profile.CardPool} ({profile.B}/{profile.A}/{profile.Q}/{profile.EX}) (Fourth number is EX attack)")
                .AppendLine($"**Max ATK:** {profile.Atk}")
                .AppendLine($"**Max HP:** {profile.HP}")
                .AppendLine($"**Starweight:** {profile.Starweight}")
                .AppendLine($"**Growth type:** {profile.GrowthCurve} (Use `.curve` for explanation)")
                .AppendLine($"**NP:** {profile.NoblePhantasm} - *{profile.NoblePhantasmEffect}*{aoe}")
                .AppendWhen(() => !String.IsNullOrWhiteSpace(profile.NoblePhantasmRankUpEffect),
                    b => b.AppendLine($"**NP Rank+:** *{profile.NoblePhantasmRankUpEffect}*{aoe}"))
                .AppendLine($"**Attribute:** {profile.Attribute}")
                .AppendLine($"**Traits:** {String.Join(", ", profile.Traits)}")
                .AppendSequence(profile.ActiveSkills,
                    (b, s) =>
                    {
                        var t = b.AppendWhen(() => !String.IsNullOrWhiteSpace(s.SkillName),
                            bu => bu.AppendLine($"**Skill {a}:** {s.SkillName} {s.Rank} - *{s.Effect}*")
                                .AppendWhen(() => !String.IsNullOrWhiteSpace(s.RankUpEffect),
                                    stb => stb.AppendLine($"**Skill {a} Rank+:** *{s.RankUpEffect}*")));
                        a++;
                        return t;
                    })
                .AppendSequence(profile.PassiveSkills,
                    (b, s) =>
                    {
                        var t = b.AppendWhen(() => !String.IsNullOrWhiteSpace(s.SkillName),
                            bu => bu.AppendLine($"**Passive Skill {p}:** {s.SkillName} {s.Rank} - *{s.Effect}*"));
                        p++;
                        return t;
                    })
                    .AppendWhen(() => !String.IsNullOrWhiteSpace(profile.Additional),
                        b => b.AppendLine($"**Additional info:** {profile.Additional}"))
                    .Append(profile.Image)
                    .AppendWhen(() => profile.Id == -3, b => b.Append("~~"))
                    .ToString();
        }

        public static string FormatMysticCodeProfile(MysticCode code)
        {
            StringBuilder sb = new StringBuilder()
                .AppendLine($"**Name:** {code.Code}")
                .AppendLine($"**Skill 1:** {code.Skill1} - *{code.Skill1Effect}*")
                .AppendLine($"**Skill 2:** {code.Skill2} - *{code.Skill2Effect}*")
                .AppendLine($"**Skill 3:** {code.Skill3} - *{code.Skill3Effect}*")
                .Append(code.Image);
            return sb.ToString();
        }

        private static Timer LoginBonusTimer;

        private enum SupportClass
        {
            Omni,
            Saber,
            Archer,
            Lancer,
            Rider,
            Caster,
            Assassin,
            Berserker
        }

        private static decimal SimulateDmg(ServantProfile srv, string enemyClass, int servantAtk, Card atkCard, int atkIndex)
        {
            var npDamageMultiplier = 1.0m;
            var firstCardBonus = atkCard == Card.Buster ? 0.5m : 0m;
            var cardDamageValue = calcCardDmg(atkCard, (--atkIndex));
            var cardMod = 0m;
            var classAtkBonus = getClassBonus(srv.Class);
            var triangleModifier = getTriMod(srv.Class, enemyClass);
            var attributeModifier = 1.0m;
            var rng = new Random();

            double rand;
            do rand = rng.NextDouble();
            while (rand < 0.9d || rand > 1.1d);

            var randomModifier = Convert.ToDecimal(rand);
            var atkMod = 0m;
            var defMod = 0m;
            var criticalModifier = 1.0m;
            var extraCardModifier = atkCard == Card.Extra ? 2.0m : 1.0m;
            var powerMod = 0m;
            var selfDamageMod = 0m;
            var critDamageMod = 0m;
            var isCrit = 0;
            var npDamageMod = 1.0m;
            var isNP = 0;
            var superEffectiveModifier = 1.0m;
            var isSuperEffective = 0;
            var dmgPlusAdd = 0;
            var selfDmgCutAdd = 0;
            var busterChainMod = 0;

            return (servantAtk * npDamageMultiplier *
                (firstCardBonus +
                    (cardDamageValue *
                        (1 + cardMod))) *
                classAtkBonus * triangleModifier * attributeModifier * randomModifier * 0.23m *
                (1 + atkMod - defMod) *
                criticalModifier * extraCardModifier *
                (1 + powerMod + selfDamageMod +
                    (critDamageMod * isCrit) +
                    (npDamageMod * isNP)) *
                (1 + ((superEffectiveModifier - 1) *
                    isSuperEffective))) +
            dmgPlusAdd + selfDmgCutAdd + (servantAtk * busterChainMod);
        }

        private static decimal getClassBonus(string srvClass)
        {
            switch (srvClass)
            {
                case "Archer":
                    return 0.95m;
                case "Caster":
                case "Assassin":
                    return 0.9m;
                case "Saber":
                case "Rider":
                case "Shielder":
                case "Alter-Ego":
                case "Beast":
                default:
                    return 1.0m;
                case "Lancer":
                    return 1.05m;
                case "Berserker":
                case "Ruler":
                case "Avenger":
                    return 1.1m;
            }
        }

        private static decimal getTriMod(string attacker, string defender)
        {
            switch (attacker)
            {
                case "Saber":
                    switch (defender)
                    {
                        case "Archer":
                        case "Ruler":
                            return 0.5m;
                        case "Saber":
                        case "Rider":
                        case "Caster":
                        case "Assassin":
                        case "Shielder":
                        case "Beast":
                        default:
                            return 1.0m;
                        case "Lancer":
                        case "Berserker":
                        case "Alter-Ego":
                        case "Avenger":
                            return 2.0m;
                    }
                case "Archer":
                    switch (defender)
                    {
                        case "Lancer":
                        case "Ruler":
                            return 0.5m;
                        case "Archer":
                        case "Caster":
                        case "Assassin":
                        case "Rider":
                        case "Shielder":
                        case "Beast":
                        default:
                            return 1.0m;
                        case "Saber":
                        case "Berserker":
                        case "Alter-Ego":
                        case "Avenger":
                            return 2.0m;
                    }
                case "Lancer":
                    switch (defender)
                    {
                        case "Saber":
                        case "Ruler":
                            return 0.5m;
                        case "Lancer":
                        case "Rider":
                        case "Caster":
                        case "Assassin":
                        case "Shielder":
                        case "Beast":
                        default:
                            return 1.0m;
                        case "Archer":
                        case "Berserker":
                        case "Alter-Ego":
                        case "Avenger":
                            return 2.0m;
                    }
                case "Rider":
                    switch (defender)
                    {
                        case "Assassin":
                        case "Ruler":
                            return 0.5m;
                        case "Saber":
                        case "Archer":
                        case "Lancer":
                        case "Rider":
                        case "Shielder":
                        case "Alter-Ego":
                        default:
                            return 1.0m;
                        case "Caster":
                        case "Berserker":
                        case "Avenger":
                        case "Beast":
                            return 2.0m;
                    }
                case "Caster":
                    switch (defender)
                    {
                        case "Rider":
                        case "Ruler":
                            return 0.5m;
                        case "Saber":
                        case "Archer":
                        case "Lancer":
                        case "Caster":
                        case "Shielder":
                        default:
                            return 1.0m;
                        case "Assassin":
                        case "Berserker":
                        case "Alter-Ego":
                        case "Avenger":
                        case "Beast":
                            return 2.0m;
                    }
                case "Assassin":
                    switch (defender)
                    {
                        case "Caster":
                        case "Ruler":
                            return 0.5m;
                        case "Saber":
                        case "Archer":
                        case "Lancer":
                        case "Assassin":
                        case "Shielder":
                        default:
                            return 1.0m;
                        case "Rider":
                        case "Berserker":
                        case "Alter-Ego":
                        case "Avenger":
                        case "Beast":
                            return 2.0m;
                    }
                case "Berserker":
                    return defender == "Shielder" ? 1.0m : 1.5m;
                case "Shielder":
                    return 1.0m;
                case "Ruler":
                    switch (defender)
                    {
                        case "Saber":
                        case "Archer":
                        case "Lancer":
                        case "Rider":
                        case "Caster":
                        case "Assassin":
                        case "Shielder":
                        case "Ruler":
                        case "Alter-Ego":
                        case "Beast":
                        default:
                            return 1.0m;
                        case "Berserker":
                        case "Avenger":
                            return 2.0m;
                    }
                case "Alter-Ego":
                    switch (defender)
                    {
                        case "Saber":
                        case "Archer":
                        case "Lancer":
                        case "Shielder":
                        case "Ruler":
                        case "Alter-Ego":
                        case "Beast":
                        default:
                            return 1.0m;
                        case "Rider":
                        case "Caster":
                        case "Assassin":
                        case "Berserker":
                        case "Avenger":
                            return 2.0m;
                    }
                case "Avenger":
                    switch (defender)
                    {
                        case "Saber":
                        case "Archer":
                        case "Lancer":
                        case "Rider":
                        case "Caster":
                        case "Assassin":
                        case "Shielder":
                        case "Alter-Ego":
                        case "Avenger":
                        case "Beast":
                        default:
                            return 1.0m;
                        case "Berserker":
                        case "Ruler":
                            return 2.0m;
                    }
                case "Beast":
                    switch (defender)
                    {
                        case "Avenger":
                            return 0.5m;
                        case "Rider":
                        case "Caster":
                        case "Assassin":
                        case "Shielder":
                        case "Ruler":
                        case "Alter-Ego":
                        case "Beast":
                        default:
                            return 1.0m;
                        case "Saber":
                        case "Archer":
                        case "Lancer":
                        case "Berserker":
                            return 2.0m;
                    }
                default:
                    return 1.0m;
            }
        }

        private static decimal calcCardDmg(Card atk, int index)
        {
            switch (atk)
            {
                case Card.Arts:
                    return 1.0m + (0.2m * index);
                case Card.Buster:
                    return 1.5m + (0.3m * index);
                case Card.Quick:
                    return 0.8m + (0.16m * index);
                default:
                    return 1.0m;
            }
        }
    }
}
