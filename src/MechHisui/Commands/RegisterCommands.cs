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
using Discord.Modules;
using Newtonsoft.Json;
using MechHisui.Modules;

namespace MechHisui.Commands
{
    public static class RegisterCommands
    {
        public static void RegisterAddAliasCommand(this DiscordClient client, IConfiguration config)
        {
            client.Commands().CreateCommand("addalias")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && ch.Id == Int64.Parse(config["FGO_general"]))
                .Hide()
                .Parameter("servant", ParameterType.Required)
                .Parameter("alias", ParameterType.Required)
                .Do(async cea =>
                {
                    var newAlias = StatService.servantDict.SingleOrDefault(p => p.Servant == cea.Args[0]);
                    if (newAlias != null)
                    {
                        newAlias.Alias.Add(cea.Args[1]);
                        using (TextWriter tw = new StreamWriter(config["ServantAliasPath"]))
                        {
                            tw.Write(JsonConvert.SerializeObject(StatService.servantDict, Formatting.Indented));
                        }
                        await client.SendMessage(cea.Channel, $"Added alias `{cea.Args[1]}` for `{newAlias.Servant}`.");
                    }
                    else
                    {
                        await client.SendMessage(cea.Channel, "Could not find name to add alias for.");
                    }
                });
        }

        public static void RegisterAddChannelCommand(this DiscordClient client, IConfiguration config)
        {
            client.Commands().CreateCommand("add")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Hide()
                .Parameter("id", ParameterType.Required)
               // .Parameter("services", ParameterType.Multiple)
                .Do(async cea =>
                {
                    long ch;
                    if (Int64.TryParse(cea.Args[0], out ch))
                    {
                        Channel chan = client.GetChannel(ch);
                        client.Modules().Modules
                            .SingleOrDefault(m => m.Id == nameof(ChannelWhitelistModule).ToLowerInvariant())
                            .EnableChannel(chan);

                        //if (cea.Args.Length > 1)
                        //{
                        //    List<string> services = new List<string>();
                        //    for (int i = 1; i < cea.Args.Length; i++)
                        //    {
                        //        if (cea.Args[i] == nameof(Responder).ToLowerInvariant())
                        //        {
                        //            client.MessageReceived += (new Responder(chan, client).Respond);
                        //        }
                        //        else if (cea.Args[i] == nameof(Recorder).ToLowerInvariant())
                        //        {

                        //        }
                        //        else
                        //        {
                        //            continue;
                        //        }
                        //        services.Add(cea.Args[i]);
                        //    }

                        //    await client.SendMessage(cea.Channel, $"Now listening on channel {chan.Name} in {chan.Server.Name} with {String.Join(", ", services)} until next shutdown.");
                        //}
                        //else
                        //{
                        //    await client.SendMessage(cea.Channel, $"Now listening on channel {chan.Name} in {chan.Server.Name} until next shutdown.");
                        //}

                        client.MessageReceived += (new Responder(chan, client).Respond);
                        await client.SendMessage(cea.Channel, $"Now listening on channel `{chan.Name}` in `{chan.Server.Name}` until next shutdown.");
                        await client.SendMessage(chan, config["Hello"]);
                    }
                    else
                    {
                        await client.SendMessage(cea.Channel, "Could not parse channel ID."); 
                    }
                });
        }

        public static void RegisterDailyCommand(this DiscordClient client, IConfiguration config)
        {
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

        public static void RegisterDisconnectCommand(this DiscordClient client, IConfiguration config)
        {
            client.Commands().CreateCommand("disconnect")
                .AddCheck((c, u, ch) => u.Id == long.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Hide()
                .Do(async cea =>
                {
                    await Disconnect(client, config);
                });
        }

        public static void RegisterFriendsCommand(this DiscordClient client, IConfiguration config)
        {
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

        public static void RegisterInfoCommand(this DiscordClient client, IConfiguration config)
        {
            client.Commands().CreateCommand("info")
                .AddCheck((c, u, ch) =>  Helpers.IsWhilested(ch, client))
                .Description("Relay info about myself.")
                .Do(async cea =>
                {
                    await client.SendMessage(cea.Channel, "I am a bot made by Joe4evr. Find my source code here: https://github.com/Joe4evr/MechHisui/ ");
                });
        }

        public static void RegisterKnownChannelsCommand(this DiscordClient client, IConfiguration config)
        {
            client.Commands().CreateCommand("known")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Hide()
                .Do(async cea =>
                {
                    foreach (var channel in Helpers.IterateChannels(client.AllServers, printServerNames: true, printChannelNames: false))                    {
                        Console.WriteLine($"{channel.Name}:  {channel.Id}");
                    }
                    await client.SendMessage(cea.Channel, "Known Channel IDs logged to console.");
                });
        }

        public static void RegisterLearnCommand(this DiscordClient client, IConfiguration config)
        {
            client.Commands().CreateCommand("learn")
                .AddCheck((c, u, ch) => u.Id == long.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Parameter("trigger", ParameterType.Required)
                .Parameter("response", ParameterType.Required)
                .Parameter("kind", ParameterType.Optional)
                .Hide()
                .Do(async cea =>
                {
                    string triggger = cea.Args[0];
                    string response = cea.Args[1];
                    //var response = new Response { Call = new[] { cea.Args[0] }, Resp = new[] { cea.Args[1] } };
                    Responses.responseDict.AddOrUpdate(
                        Responses.responseDict.SingleOrDefault(kv => kv.Key.Contains(triggger)).Key ?? new string[] { triggger },
                        new string[] { response },
                        (k,v) =>
                        {
                            var t = v.ToList();
                            t.Add(response);
                            return t.ToArray();
                        });
                    using (TextWriter tw = new StreamWriter(config["ResponsesPath"]))
                    {
                        var l = new List<Response>();
                        foreach (var item in Responses.responseDict)
                        {
                            l.Add(new Response { Call = item.Key, Resp = item.Value });
                        }
                        tw.Write(JsonConvert.SerializeObject(l, Formatting.Indented));
                    }
                    await client.SendMessage(cea.Channel, $"Understood. Shall respond to `{triggger}` with `{response}`.");
                });
        }

        public static void RegisterMarkCommand(this DiscordClient client, IConfiguration config)
        {
            client.Commands().CreateCommand("mark")
                .AddCheck((c, u, ch) => u.Id == long.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Hide()
                .Do(async cea =>
                {
                    Console.WriteLine($"Marked at {DateTime.Now}");
                    await client.SendMessage(cea.Channel, "Marked current activity in the console.");
                });
        }

        public static void RegisterRecording(this DiscordClient client, IConfiguration config)
        {
            client.Commands().CreateCommand("record")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Hide()
                .Do(async cea =>
                {
                    var rec = client.GetRecordingChannels();
                    if (rec.Contains(cea.Channel.Id))
                    {
                        await client.SendMessage(cea.Channel, $"Already recording here.");
                    }
                    else
                    {
                        rec.Add(cea.Channel.Id);
                        var recorder = new Recorder(cea.Channel, client, config);
                        client.GetRecorders().Add(recorder);
                    }
                });

            client.Commands().CreateCommand("endrecord")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Hide()
                .Do(async cea =>
                {
                    var rec = client.GetRecordingChannels();
                    if (!rec.Contains(cea.Channel.Id))
                    {
                        await client.SendMessage(cea.Channel, $"Not recording here.");
                    }
                    else
                    {
                        var recorder = client.GetRecorders().Single(r => r.channel.Id == cea.Channel.Id);
                        await recorder.EndRecord(client);
                        client.GetRecorders().Remove(recorder);
                        rec.Remove(cea.Channel.Id);
                    }
                });
        }

        public static void RegisterResetCommand(this DiscordClient client, IConfiguration config)
        {
            client.Commands().CreateCommand("reset")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Hide()
                .Do(async cea =>
                {
                    var resp = client.GetResponders().Single(r => r.channel.Id == cea.Channel.Id);
                    resp.ResetTimeouts();
                    await client.SendMessage(cea.Channel, "Timeouts reset.");
                });
        }

        public static void RegisterStatsCommand(this DiscordClient client, IConfiguration config, StatService stats)
        {
            stats._servantProfiles.Add(new ServantProfile
            {
                Name = "Mech-Hisui",
                Class = "Pleasant-type City Subjugation Weapon",
                Rarity = "5☆",
                Id = -10,
                CardPool = "BBAAQ",
                Atk = 11137,
                HP = 10304,
                GrowthCurve = "Linear",
                NoblePhantasm = "(Buster) Saturday Night Forever",
                NoblePhantasmEffect = "Chance to Petrify (40%-90%) 1T, Dmg (1000%-1500%)",
                Skill1 = "Kohaku Barrier B",
                Effect1 = "Self Def+ (9%-18%) 3T CD:8",
                Skill2 = "Execution Laser A",
                Effect2 = "Inflict Burn status (500-1000 Dmg) 5T CD:8",
                PassiveSkill1 = "Magic Resistance A",
                PEffect1 = "Debuff Resist+ 20%",
                PassiveSkill2 = "Independent Action B",
                PEffect2 = "Critical Dmg+ 8%"
            });
            StatService.servantDict.Add(new ServantAlias { Alias = new List<string> { "mechhisui", "mech hisui", "mech-hisui" }, Servant = "Mech-Hisui" });

            client.Commands().CreateCommand("stats")
                .AddCheck((c, u, ch) => ch.Id == Int64.Parse(config["FGO_general"]))
                .Parameter("servantname", ParameterType.Multiple)
                .Description($"Relay information on the specified Servant. Alternative names acceptable. *Currently up to {stats._servantProfiles.Count(p => !String.IsNullOrWhiteSpace(p.NoblePhantasm))}/{stats._servantProfiles.Count}.*")
                .Do(async cea =>
                {
                    var arg = String.Join(" ", cea.Args);
                    if (new[] { "waifu", "mai waifu", "my waifu" }.Contains(arg.ToLowerInvariant()))
                    {
                        await client.SendMessage(cea.Channel, "It has come to my attention that your 'waifu' is equatable to fecal matter.");
                        return;
                    }

                    if ((new[] { "jeanne alter", "ruler alter"}).Contains(arg.ToLowerInvariant()))
                    {
                        var sp = new ServantProfile
                        {
                            Id = 1000,
                            Class = "Ruler",
                            Rarity = "4☆ (unobtainable)",
                            Name = "Jeanne d'Arc (Alter)",
                            Atk = 9804,
                            HP = 11137,
                            CardPool = "BBAAQ"
                        };
                        await client.SendMessage(cea.Channel, Helpers.FormatServantProfile(sp));
                        return;
                    }


                    var profile = stats.LookupStats(arg);
                    if (profile != null)
                    {
                        await client.SendMessage(cea.Channel, Helpers.FormatServantProfile(profile)); 
                    }
                    else
                    {
                        var name = stats.LookupServantName(arg);
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
        }

        public static void RegisterTriviaCommand(this DiscordClient client, IConfiguration config)
        {
            client.Commands().CreateCommand("trivia")
               .AddCheck((c, u, ch) => ch.Id == long.Parse(config["FGO_trivia"]))
               .Parameter("rounds", ParameterType.Required)
               .Description("Would you like to play a game?")
               .Do(async cea =>
               {
                   if (client.GetTrivias().Any(t => t.Channel.Id == cea.Channel.Id))
                   {
                       await client.SendMessage(cea.Channel, $"Trivia already running.");
                       return;
                   }
                   int rounds;
                   if (int.TryParse(cea.Args[0], out rounds))
                   {
                       if (rounds > TriviaHelpers.Questions.Count)
                       {
                           await client.SendMessage(cea.Channel, $"Could not start trivia, too many questions specified.");
                       }
                       else
                       {
                           await client.SendMessage(cea.Channel, $"Starting trivia. Play until {rounds} points to win.");
                           var trivia = new Trivia(client, rounds, cea.Channel, config);
                           client.GetTrivias().Add(trivia);
                           trivia.StartTrivia();
                       }
                   }
                   else
                   {
                       await client.SendMessage(cea.Channel, $"Could not start trivia, parameter was not a number.");
                   }
               });
        }

        public static void RegisterUpdateCommand(this DiscordClient client, IConfiguration config, StatService stats)
        {
            client.Commands().CreateCommand("update")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Parameter("item", ParameterType.Optional)
                .Hide()
                .Do(async cea =>
                {
                    switch (cea.Args[0])
                    {
                        case "alias":
                            stats.ReadAliasList(config);
                            await client.SendMessage(cea.Channel, "Updated alias lookup.");
                            break;
                        case "profiles":
                            stats.UpdateProfileList(config);
                            await client.SendMessage(cea.Channel, "Updated profile lookup.");
                            break;
                        default:
                            stats.ReadAliasList(config);
                            stats.UpdateProfileList(config);
                            await client.SendMessage(cea.Channel, "Updated all lookups.");
                            break;
                    }
                });
        }

        public static void RegisterWhereCommand(this DiscordClient client, IConfiguration config)
        {
            client.Commands().CreateCommand("where")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Parameter("item")
                .Hide()
                .Do(async cea =>
                {
                    ChannelActivity ca;
                    StringBuilder sb = new StringBuilder();
                    if (Enum.TryParse(cea.Args[0], ignoreCase: true, result: out ca))
                    {
                        switch (ca)
                        {
                            case ChannelActivity.Recorder:
                                sb.AppendLine("Currently recording in: ");
                                foreach (var item in client.GetRecorders())
                                {
                                    sb.AppendLine($"{item.channel.Server.Name} - {item.channel.Name}");
                                }
                                break;
                            case ChannelActivity.Responder:
                                sb.AppendLine("Currently responding in: ");
                                foreach (var item in client.GetResponders())
                                {
                                    sb.AppendLine($"{item.channel.Server.Name} - {item.channel.Name}");
                                }
                                break;
                            case ChannelActivity.Trivia:
                                sb.AppendLine("Currently holding trivia in: ");
                                foreach (var item in client.GetTrivias())
                                {
                                    sb.AppendLine($"{item.Channel.Server.Name} - {item.Channel.Name}");
                                }
                                break;
                            default:
                                break;
                        }
                        var str = sb.ToString();
                        if (!String.IsNullOrWhiteSpace(str))
                        {
                            await client.SendMessage(cea.Channel, str);
                        }
                    }
                    else
                    {
                        await client.SendMessage(cea.Channel, "Invalid Argument.");
                    }
                });
        }

        internal static async Task Disconnect(DiscordClient client, IConfiguration config)
        {
            StopReponders(client, client.GetResponders());
            await StopRecorders(client, client.GetRecorders());
            //await StopTrvias(client.GetTrivias());

            foreach (var ch in client.Modules().Modules
                .Single(m => m.Id == nameof(ChannelWhitelistModule).ToLowerInvariant())
                .EnabledChannels)
            {
                if (ch.Id != Int64.Parse(config["API_testing"]))
                {
                    await client.SendMessage(ch, config["Goodbye"]);
                }
            }

            Environment.Exit(0);
        }

        private static async Task StopTrvias(List<Trivia> trivs)
        {
            if (trivs.Any())
            {
                foreach (var triv in trivs)
                {
                    await triv.EndTriviaEarly();
                }
            }
        }

        private static async Task StopRecorders(DiscordClient client, List<Recorder> recs)
        {
            if (recs.Any())
            {
                foreach (var rec in recs)
                {
                    await rec.EndRecord(client);
                }
            }
        }

        private static void StopReponders(DiscordClient client, List<Responder> resps)
        {
            if (resps.Any())
            {
                foreach (var resp in resps)
                {
                    client.MessageReceived -= resp.Respond;
                }
            }
        }

        private enum ChannelActivity
        {
            Recorder,
            Responder,
            Trivia
        }
    }
}
