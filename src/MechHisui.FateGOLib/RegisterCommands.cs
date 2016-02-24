using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Commands;
using JiiLib;
using JiiLib.Net;
using Newtonsoft.Json;
using MechHisui.FateGOLib;

namespace MechHisui.Commands
{
    public static class ClientExtensions
    {
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
                        await cea.Channel.SendMessage("One or both arguments could not be parsed.");
                    }
                });
        }

        public static void RegisterDailyCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Daily'...");
            client.GetService<CommandService>().CreateCommand("daily")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"]))
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
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"]) || ch.Id == UInt64.Parse(config["FGO_events"]))
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
            Console.WriteLine("Registering 'Friends'...");
            FriendCodes.ReadFriendData(config["FriendcodePath"]);
            client.GetService<CommandService>().CreateCommand("friendcode")
               .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"]))
               .Parameter("code", ParameterType.Required)
               .Parameter("servant", ParameterType.Optional)
               .Description("Add your friendcode to the list. Enter your code with quotes as `\"XXX XXX XXX\"`. You may optionally add your support Servant as well. If you do, enclose that in `\"\"`s as well.")
               .Do(async cea =>
               {
                   if (FriendCodes.friendData.Any(fc => fc.User == cea.User.Name))
                   {
                       await cea.Channel.SendMessage($"Already in the Friendcode list. Please use `.updatefc` to update your description.");
                       return;
                   }

                   if (Regex.Match(cea.Args[0], @"[0-9][0-9][0-9] [0-9][0-9][0-9] [0-9][0-9][0-9]").Success)
                   {
                       var friend = new FriendData { Id = FriendCodes.friendData.Count + 1, User = cea.User.Name, FriendCode = cea.Args[0], Servant = (cea.Args.Length > 1) ? cea.Args[1] : String.Empty };
                       FriendCodes.friendData.Add(friend);
                       FriendCodes.WriteFriendData(config["FriendcodePath"]);
                       await cea.Channel.SendMessage($"Added `{friend.FriendCode}` for `{friend.User}`.");
                   }
                   else
                   {
                       await cea.Channel.SendMessage($"Incorrect friendcode format specified.");
                   }
               });

            client.GetService<CommandService>().CreateCommand("listcodes")
               .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_playground"]))
               .Description("Display known friendcodes.")
               .Do(async cea =>
               {
                   var sb = new StringBuilder("```\n");
                   int longestName = FriendCodes.friendData.OrderByDescending(f => f.User.Length).First().User.Length;
                   foreach (var friend in FriendCodes.friendData.OrderBy(f => f.Id))
                   {
                       var spaces = new string(' ', (longestName - friend.User.Length) + 1);
                       sb.Append($"{friend.User}:{spaces}{friend.FriendCode}");
                       sb.AppendLine((!String.IsNullOrEmpty(friend.Servant)) ? $" - {friend.Servant}" : String.Empty);
                       if (sb.Length > 1700)
                       {
                           sb.Append("\n```");
                           await cea.Channel.SendMessage(sb.ToString());
                           sb = new StringBuilder("```\n");
                       }
                   }
                   sb.Append("\n```");
                   await cea.Channel.SendMessage(sb.ToString());
               });

            client.GetService<CommandService>().CreateCommand("updatefc")
               .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"]))
               .Parameter("newServant", ParameterType.Unparsed)
               .Description("Update the Support Servant displayed in your friendcode listing.")
               .Do(async cea =>
               {
                   var arg = cea.Args[0];
                   if (arg.Length == 0)
                   {
                       await cea.Channel.SendMessage($"No argument specified.");
                       return;
                   }

                   Func<FriendData, bool> pred = c => c.User == cea.User.Name;
                   if (FriendCodes.friendData.Any(pred))
                   {
                       var temp = FriendCodes.friendData.Single(pred);
                       FriendCodes.friendData.Remove(FriendCodes.friendData.Single(pred));
                       temp.Servant = arg;
                       FriendCodes.friendData.Add(temp);
                       FriendCodes.WriteFriendData(config["FriendcodePath"]);
                       await cea.Channel.SendMessage($"Updated `{temp.User}`'s Servant to be `{temp.Servant}`.");
                   }
                   else
                   {
                       await cea.Channel.SendMessage("Profile not found. Please add your profile using `.friendcode`.");
                   }
               });
        }

        public static void RegisterLoginBonusCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Login bonus'...");
            client.GetService<CommandService>().CreateCommand("login")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"]))
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
                .TimeUntilNextLocalTimeAt(new TimeSpan(4, 0, 0)),
            TimeSpan.FromDays(1));
        }

        public static void RegisterQuartzCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Quartz'...");
            client.GetService<CommandService>().CreateCommand("quartz")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"]))
                .Description("Relay the prices of different amounts of Saint Quartz.")
                .Do(async cea =>
                    await cea.Channel.SendMessage(
@"Prices for Quartz:
```
  1Q: 120 JPY
  5Q: 480 JPY
 16Q: 1400 JPY
 36Q: 2900 JPY
 65Q: 4800 JPY
140Q: 9800 JPY
```"));
        }

        public static void RegisterStatsCommands(this DiscordClient client, IConfiguration config)
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
                //Using .Wait() here since there is no proper async context that await works
                statService.UpdateProfileListsAsync().Wait();
                statService.UpdateCEListAsync().Wait();
                statService.UpdateEventListAsync().Wait();
                statService.UpdateMysticCodesListAsync().Wait();
                statService.UpdateDropsListAsync().Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(0);
            }

            Console.WriteLine("Registering 'Stats'...");
            client.GetService<CommandService>().CreateCommand("stats")
                .Alias("stat")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"]))
                .Parameter("servantname", ParameterType.Unparsed)
                .Description($"Relay information on the specified Servant. Alternative names acceptable.")
                .Do(async cea =>
                {
                    var arg = String.Join(" ", cea.Args);
                    if (arg.ContainsIgnoreCase("waifu"))
                    {
                        await cea.Channel.SendMessage("It has come to my attention that your 'waifu' is equatable to fecal matter.");
                        return;
                    }

                    if (new[] { "enkidu", "arc", "arcueid", "astolfo" }.ContainsIgnoreCase(arg))
                    {
                        await cea.Channel.SendMessage("Never ever.");
                        return;
                    }

                    ServantProfile profile;
                    int id;
                    if (Int32.TryParse(arg, out id))
                    {
                        profile = FgoHelpers.ServantProfiles.SingleOrDefault(p => p.Id == id);
                    }
                    else
                    {
                        profile = statService.LookupStats(arg);
                    }

                    if (profile != null)
                    {
                        await cea.Channel.SendMessage(FormatServantProfile(profile));
                    }
                    else
                    {
                        var name = statService.LookupServantName(arg);
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
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"]))
                .Parameter("cename", ParameterType.Unparsed)
                .Description($"Relay information on the specified Craft Essence. Alternative names acceptable.")
                .Do(async cea =>
                {
                    var arg = String.Join(" ", cea.Args);

                    var ce = statService.LookupCE(arg);
                    if (ce != null)
                    {
                        await cea.Channel.SendMessage(FormatCEProfile(ce));
                    }
                    else
                    {
                        var potentials = FgoHelpers.CEDict.Where(c => c.Alias.Any(a => a.ContainsIgnoreCase(arg)) || c.CE.ContainsIgnoreCase(arg));
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

            Console.WriteLine("Registering 'All CE'...");
            client.GetService<CommandService>().CreateCommand("allce")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"]))
                .Parameter("ceeffect", ParameterType.Unparsed)
                .Description($"Relay information on CEs having the specified effect.")
                .Do(async cea =>
                {
                    var arg = String.Join(" ", cea.Args);

                    var ces = FgoHelpers.CEProfiles.Where(c => c.Effect.ContainsIgnoreCase(arg));
                    if (ces.Count() > 0)
                    {
                        string matches = String.Join("\n", ces.Select(c => $"**{c.Name}** - {c.Effect}"));
                        await cea.Channel.SendMessage(matches);
                    }
                    else
                    {
                        await cea.Channel.SendMessage("No such CEs found. Please try another term.");
                    }
                });

            Console.WriteLine("Registering 'Update'...");
            client.GetService<CommandService>().CreateCommand("update")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && (ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"])))
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
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && (ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"])))
                .Hide()
                .Parameter("servant", ParameterType.Required)
                .Parameter("alias", ParameterType.Required)
                .Do(async cea =>
                {
                    ServantAlias newAlias = FgoHelpers.ServantDict.SingleOrDefault(p => p.Servant == cea.Args[0]);
                    if (newAlias != null)
                    {
                        newAlias.Alias.Add(cea.Args[1]);
                    }
                    else
                    {
                        ServantProfile profile = FgoHelpers.ServantProfiles.SingleOrDefault(s => s.Name == cea.Args[0]);
                        if (profile != null)
                        {
                            newAlias = new ServantAlias
                            {
                                Alias = new List<string> { cea.Args[1] },
                                Servant = profile.Name
                            };
                        }
                        else
                        {
                            await cea.Channel.SendMessage("Could not find name to add alias for.");
                            return;
                        }
                    }
                    using (TextWriter tw = new StreamWriter(Path.Combine(config["AliasPath"], "servants.json")))
                    {
                        tw.Write(JsonConvert.SerializeObject(FgoHelpers.ServantDict, Formatting.Indented));
                    }
                    await cea.Channel.SendMessage($"Added alias `{cea.Args[1]}` for `{newAlias.Servant}`.");
                });

            Console.WriteLine("Registering 'CE alias'...");
            client.GetService<CommandService>().CreateCommand("cealias")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && (ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"])))
                .Hide()
                .Parameter("ce", ParameterType.Required)
                .Parameter("alias", ParameterType.Required)
                .Do(async cea =>
                {
                    CEAlias newAlias = FgoHelpers.CEDict.SingleOrDefault(p => p.CE == cea.Args[0]);
                    if (newAlias != null)
                    {
                        newAlias.Alias.Add(cea.Args[1]);
                    }
                    else
                    {
                        CEProfile ce = FgoHelpers.CEProfiles.SingleOrDefault(s => s.Name == cea.Args[0]);
                        if (ce != null)
                        {
                            newAlias = new CEAlias
                            {
                                Alias = new List<string> { cea.Args[1] },
                                CE = ce.Name
                            };
                        }
                        else
                        {
                            await cea.Channel.SendMessage("Could not find name to add alias for.");
                            return;
                        }
                    }
                    using (TextWriter tw = new StreamWriter(Path.Combine(config["AliasPath"], "ces.json")))
                    {
                        tw.Write(JsonConvert.SerializeObject(FgoHelpers.CEDict, Formatting.Indented));
                    }
                    await cea.Channel.SendMessage($"Added alias `{cea.Args[1]}` for `{newAlias.CE}`.");
                });

            Console.WriteLine("Registering 'Curve'...");
            client.GetService<CommandService>().CreateCommand("curve")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"]))
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
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"]))
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
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"]))
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
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && (ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"])))
                .Hide()
                .Parameter("mystic", ParameterType.Required)
                .Parameter("alias", ParameterType.Required)
                .Do(async cea =>
                {
                    var newAlias = FgoHelpers.MysticCodeDict.SingleOrDefault(p => p.Code == cea.Args[0]);
                    if (newAlias != null)
                    {
                        newAlias.Alias.Add(cea.Args[1]);
                    }
                    else
                    {
                        await cea.Channel.SendMessage("Could not find Mystic Code to add alias for.");
                        return;
                    }
                    using (TextWriter tw = new StreamWriter(Path.Combine(config["AliasPath"], "mystic.json")))
                    {
                        tw.Write(JsonConvert.SerializeObject(FgoHelpers.CEDict, Formatting.Indented));
                    }
                    await cea.Channel.SendMessage($"Added alias `{cea.Args[1]}` for `{newAlias.Code}`.");
                });

            Console.WriteLine("Registering 'Drops'...");
            client.GetService<CommandService>().CreateCommand("drops")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"]))
                .Description("Relay information about item drop locations.")
                .Parameter("item", ParameterType.Unparsed)
                .Do(async cea =>
                {
                    var arg = String.Join(" ", cea.Args);
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
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"]))
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

                    await cea.Channel.SendMessage(
$@"**Team Saber:** {hgw.ElementAt(0).Key} + {hgw.ElementAt(0).Value}
**Team Archer:** {hgw.ElementAt(1).Key} + {hgw.ElementAt(1).Value}
**Team Lancer:** {hgw.ElementAt(2).Key} + {hgw.ElementAt(2).Value}
**Team Rider:** {hgw.ElementAt(3).Key} + {hgw.ElementAt(3).Value}
**Team Caster:** {hgw.ElementAt(4).Key} + {hgw.ElementAt(4).Value}
**Team Assassin:** {hgw.ElementAt(5).Key} + {hgw.ElementAt(5).Value}
**Team Berserker:** {hgw.ElementAt(6).Key} + {hgw.ElementAt(6).Value}
Discuss.");
                });

            client.GetService<CommandService>().CreateCommand("addhgw")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && (ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"])))
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

            Console.WriteLine("Registering 'Roll'...");
            #region long vars
            var rolltypes = new[] { "fp1", "fp10", "ticket", "4", "40" };
            var excluded = new[]
            {
                "Jeanne d'Arc (Alter) (unobtainable)",
                "Mash Kyrielight",
                "Arturia Pendragon (Lily)",
                "Gilgamesh",
                "Sakata Kintoki",
                "Elizabeth Bathory (Halloween)",
                "Okita Souji",
                "Oda Nobunaga",
                "Scathach",
                "Arturia Pendragon (Santa Alter)",
                "Hyde",
                "Arjuna",
                "Karna",
                "Mysterious Heroine X",
                "Brynhildr",
                "Nero Claudius (Bride)",

                "Journey's Beginning",
                "Nightless Rose",
                "Moony Jewel",
                "Bathing Moon Goddess",
                "Moonlight Fest",
                "Jack-O'-Lantern",
                "Trick or Treat",
                "Halloween Arrangement",
                "Halloween Princess",
                "Halloween Petite Devil",
                "Maid in Halloween",
                "Fate GUDAGUDA Order",
                "Launch Order!",
                "GUDAGUDA Poster Girl",
                "GUDAO",
                "Okita",
                "Nobu",
                "Lightning Reindeer",
                "March of Saints",
                "Present for My Master",
                "Holy Night Sign",
                "Peace of 2016",
                "Heroic New Year",
                "Law of the Jungle",
                "Grand New Year",
                "Mona Lisa",
                "Happy x3 Order",
                "Purely Bloom",
                "Artoria Star",
                "Trueshot",
                "Mikotto! Training to be a Bride",
                "Crimson Fortress of Shadow",
                "Mysterious Lifeform Alpha",
                "Mysterious Lifeform Beta",
                "Heroic Spirit Portrait",
                "Tears of Valentine Dojo",
                "Kitchen ☆ Patissiere",
                "Street Choco-Maid",
                "Melty Sweetheart",
                "Valentine Chocolate"
            };
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
            #endregion
            client.GetService<CommandService>().CreateCommand("roll")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_playground"]))
                .Description("Simulate gacha roll (not accurate wrt rarity ratios and rate ups). Accepetable parameters are `fp1`, `fp10`, `ticket`, `4`, and `40`")
                .Parameter("what", ParameterType.Required)
                .Do(async cea =>
                {
                    if (!rolltypes.Contains(cea.Args[0]))
                    {
                        await cea.Channel.SendMessage("Unaccaptable parameter.");
                        return;
                    }

                    var rng = new Random();
                    List<string> pool;
                    List<string> picks = new List<string>();
                    if (cea.Args[0] == "fp" || cea.Args[0] == "fp10")
                    {
                        pool = FgoHelpers.ServantProfiles
                            .Where(p => p.Rarity <= 3)
                            .Concat(FgoHelpers.ServantProfiles
                                .Where(p => p.Rarity <= 2)
                                .RepeatSeq(5))
                            .Concat(FgoHelpers.ServantProfiles
                                .Where(p => p.Rarity == 1)
                                .RepeatSeq(5))
                            .Select(p => p.Name)
                            .Concat(FgoHelpers.CEProfiles
                                .Where(ce => ce.Rarity <= 3)
                                .Concat(FgoHelpers.CEProfiles
                                    .Where(ce => ce.Rarity <= 2)
                                    .RepeatSeq(5))
                                .Concat(FgoHelpers.CEProfiles
                                    .Where(ce => ce.Rarity == 1)
                                    .RepeatSeq(5))
                                .Select(ce => ce.Name))
                            .Except(excluded)
                            .ToList();
                    }
                    else //premium roll
                    {
                        pool = FgoHelpers.ServantProfiles
                            .Where(p => p.Rarity >= 3)
                            .Concat(FgoHelpers.ServantProfiles
                                .Where(p => p.Rarity >= 3 && p.Rarity <= 4)
                                .RepeatSeq(5))
                            .Concat(FgoHelpers.ServantProfiles
                                .Where(p => p.Rarity == 3)
                                .RepeatSeq(5))
                            .Select(p => p.Name)
                            .Concat(FgoHelpers.CEProfiles
                                .Where(ce => ce.Rarity >= 3)
                                .Concat(FgoHelpers.CEProfiles
                                    .Where(ce => ce.Rarity >= 3 && ce.Rarity <= 4)
                                    .RepeatSeq(5))
                                .Concat(FgoHelpers.CEProfiles
                                    .Where(ce => ce.Rarity == 3)
                                    .RepeatSeq(5))
                                .Select(ce => ce.Name))
                            .Except(excluded)
                            .Except(fpOnly)
                            .ToList();
                    }

                    for (int i = 0; i < 28; i++)
                    {
                        pool = (List<string>)pool.Shuffle();
                    }
                    
                    if (cea.Args[0] == "fp" || cea.Args[0] == "ticket" || cea.Args[0] == "4")
                    {
                        pool = (List<string>)pool.Shuffle();
                        picks.Add(pool.ElementAt(rng.Next(maxValue: pool.Count)));
                    }
                    else //10-roll
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            pool = (List<string>)pool.Shuffle();
                            picks.Add(pool.ElementAt(rng.Next(maxValue: pool.Count)));
                        }
                    }

                    await cea.Channel.SendMessage($"**{cea.User.Name} rolled:** {String.Join(", ", picks)}");
                });
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

        private static string FormatCEProfile(CEProfile ce)
        {
            StringBuilder sb = new StringBuilder()
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
                .Append(ce.Image);
            return sb.ToString();
        }

        internal static string FormatServantProfile(ServantProfile profile)
        {
            string aoe = profile.NoblePhantasmEffect.Contains("AoE") && Regex.Match(profile.NoblePhantasmEffect, "[0-9]+H").Success ? " (Hits is per enemy)" : String.Empty;
            StringBuilder sb = new StringBuilder()
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
                .AppendLine($"**NP Rank+:** *{profile.NoblePhantasmRankUpEffect}*{aoe}")
                .AppendLine($"**Attribute:** {profile.Attribute}")
                .AppendLine($"**Traits:** {String.Join(", ", profile.Traits)}");
            int a = 1;
            foreach (var skill in profile.ActiveSkills)
            {
                if (!String.IsNullOrWhiteSpace(skill.SkillName))
                {
                    sb.AppendLine($"**Skill {a}:** {skill.SkillName} {skill.Rank} - *{skill.Effect}*");
                    a++;
                }
            }
            int p = 1;
            foreach (var skill in profile.PassiveSkills)
            {
                if (!String.IsNullOrWhiteSpace(skill.SkillName))
                {
                    sb.AppendLine($"**Passive Skill {p}:** {skill.SkillName} {skill.Rank} - *{skill.Effect}*");
                    p++;
                }
            }
            sb.Append(profile.Image);
            return sb.ToString();
        }

        private static string FormatMysticCodeProfile(MysticCode code)
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
    }
}
