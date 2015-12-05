using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Commands;
using Discord.Modules;
using MechHisui.Modules;

namespace MechHisui.Commands
{
    public static class RegisterCommands
    {
        public static void RegisterResetCommand(this DiscordClient client, IConfiguration config)
        {
            client.Commands().CreateCommand("reset")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Hide()
                .Do(async cea =>
                {
                    var resp = client.GetResponders().Where(r => r.channel.Id == cea.Channel.Id).Single();
                    resp.ResetTimeouts();
                    await client.SendMessage(cea.Channel, "Timeouts reset.");
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
                        var recorder = new Recorder(cea.Channel, client);
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
                        var recorder = client.GetRecorders().Where(r => r.channel == cea.Channel).Single();
                        await recorder.EndRecord(client);
                        client.GetRecorders().Remove(recorder);
                        rec.Remove(cea.Channel.Id);
                    }
                });
        }

        public static void RegisterStatCommand(this DiscordClient client, IConfiguration config, Wikier wikier)
        {
            client.Commands().CreateCommand("stats")
                .AddCheck((c, u, ch) => ch.Id == Int64.Parse(config["FGO_general"]))
                .Parameter("servantname", ParameterType.Required)
                .Description("Relay information on the specified Servant. Alternative names acceptable. Provide names of multiple words inside quotation marks.")
                .Do(async cea =>
                {
                    var arg = cea.Args[0];
                    if (arg.ToLowerInvariant() == "waifu")
                    {
                        await client.SendMessage(cea.Channel, "It has come to my attention that your 'waifu' is equatable to fecal matter.");
                        return;
                    }
                    
                    if ((new[] { "scath", "scathach" }).Contains(arg.ToLowerInvariant()))
                    {
                        await client.SendMessage(cea.Channel, "Never ever.");
                        return;
                    }
                    
                    var profile = wikier.LookupStats(arg);
                    if (profile == null)
                    {
                        await client.SendMessage(cea.Channel, "No such entry found. Please try another name.");
                    }
                    else
                    {
                        await client.SendMessage(cea.Channel, Helpers.FormatServantProfile(profile));
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

        internal static async Task Disconnect(DiscordClient client, IConfiguration config)
        {
            StopReponders(client, client.GetResponders());
            await StopRecorders(client, client.GetRecorders());
            await StopTrvias(client.GetTrivias());

            foreach (var ch in client.Modules().Modules
                .Where(m => m.Id == nameof(ChannelWhitelistModule).ToLowerInvariant())
                .Single()
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
                    await triv.EndTrivia();
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
                    else if (!Enum.TryParse(arg, ignoreCase: true, result: out day))
                    {
                        await client.SendMessage(cea.Channel, "Could not convert argument to a day of the week. Please try again.");
                        return;
                    }

                    DailyInfo info;
                    if (DailyInfo.DailyQuests.TryGetValue(day, out info))
                    {
                        bool isToday = (day == todayInJapan.LocalTime.DayOfWeek);
                        string whatDay = isToday ? "Today" : day.ToString();
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

        public static void RegisterLearnCommand(this DiscordClient client, IConfiguration config)
        {
            client.Commands().CreateCommand("learn")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && Helpers.IsWhilested(ch, client))
                .Parameter("trigger", ParameterType.Required)
                .Parameter("response", ParameterType.Required)
                .Hide()
                .Do(async cea =>
                {
                    Responses.quickLearn.AddOrUpdate(cea.Args[0], cea.Args[1], (k,v) => v = cea.Args[1]);

                    await client.SendMessage(cea.Channel, $"Understood. Shall respond to `{cea.Args[0]}` with `{cea.Args[1]}`.");
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
                           var trivia = new Trivia(client, rounds, cea.Channel, Trivia.TriviaType.WinAt);
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
    }
}
