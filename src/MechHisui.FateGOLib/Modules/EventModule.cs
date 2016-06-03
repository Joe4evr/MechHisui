using System;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Commands;
using Discord.Modules;

namespace MechHisui.FateGOLib.Modules
{
    public class EventModule : IModule
    {
        private readonly StatService _statService;
        private readonly IConfiguration _config;

        public EventModule(StatService statService, IConfiguration config)
        {
            _statService = statService;
            _config = config;
        }

        void IModule.Install(ModuleManager manager)
        {
            Console.WriteLine("Registering 'Event'...");
            manager.Client.GetService<CommandService>().CreateCommand("event")
                .Alias("events")
                .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(_config["FGO_server"]))
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
    }
}
