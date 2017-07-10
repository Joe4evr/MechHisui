using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.SimplePermissions;
using Discord.Commands;

namespace MechHisui.SymphoXDULib
{
    public partial class XduModule
    {
        [Group("event"), Alias("events"), Permission(MinimumPermission.Everyone)]
        public class Events : ModuleBase<SocketCommandContext>
        {
            private readonly XduStatService _stats;

            public Events(XduStatService stats)
            {
                _stats = stats;
            }

            [Command]
            public Task EventCmd()
            {
                var sb = new StringBuilder();
                var events = _stats.Config.GetEvents();
                var utcNow = DateTime.UtcNow;
                var currentEvents = events.Where(e => utcNow > e.StartTime && utcNow < e.EndTime);

                if (currentEvents.Any())
                {
                    sb.Append("**Current Event(s):** ");
                    foreach (var ev in currentEvents)
                    {
                        if (ev.EndTime.HasValue)
                        {
                            string doneAt = (ev.EndTime.Value - utcNow).ToNiceString();
                            sb.AppendLine($"{ev.EventName} for {doneAt}.");
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

                var nextEvent = events.FirstOrDefault(e => e.StartTime > utcNow) ?? events.FirstOrDefault(e => !e.StartTime.HasValue);
                if (nextEvent != null)
                {
                    if (nextEvent.StartTime.HasValue)
                    {
                        string eta = (nextEvent.StartTime.Value - utcNow).ToNiceString();
                        sb.AppendLine($"**Next Event:** {nextEvent.EventName}, planned to start in {eta}.");
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
                sb.Append("Anime Janai gear never ever");
                return ReplyAsync(sb.ToString());
            }
        }
    }
}
