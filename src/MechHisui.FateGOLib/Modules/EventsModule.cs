using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Addons.SimplePermissions;
using Discord.Commands;
using Discord.Commands.Builders;
using NodaTime;
using SharedExtensions;

namespace MechHisui.FateGOLib
{
    public partial class FgoModule
    {
        [Name("Events")]
        public sealed class EventsModule : ModuleBase<ICommandContext>
        {
            private readonly FgoStatService _service;

            public EventsModule(FgoStatService service)
            {
                _service = service;
            }

            [Command("event"), Permission(MinimumPermission.Everyone)]
            [Alias("events")]
            public async Task EventCmd()
            {
                var sb = new StringBuilder();
                var utcNow = DateTime.UtcNow;
                var currentEvents = await _service.Config.GetCurrentEventsAsync().ConfigureAwait(false);

                if (currentEvents.Any())
                {
                    sb.Append("**Current Event(s):** ");
                    foreach (var ev in currentEvents)
                    {
                        if (ev.EndTime.HasValue)
                        {
                            sb.AppendLine($"(\uFF03{ev.Id}) {ev.EventName} for {(ev.EndTime.Value - utcNow).ToNiceString()}.");
                        }
                        else
                        {
                            sb.AppendLine($"(\uFF03{ev.Id}) {ev.EventName} for unknown time.");
                        }

                        if (!String.IsNullOrEmpty(ev.InfoLink))
                        {
                            sb.AppendLine($"<{ev.InfoLink}>");
                        }

                        if (ev.EventGachas.Any())
                        {
                            foreach (var gacha in ev.EventGachas)
                            {
                                if (gacha.EndTime > utcNow)
                                {
                                    if (gacha.StartTime < utcNow)
                                    {
                                        sb.AppendLine($"\tRate up on {String.Join(", ", gacha.RateUpServants)}, for {(gacha.EndTime - utcNow).ToNiceString()}");
                                    }
                                    else
                                    {
                                        sb.AppendLine($"\tRate up on {String.Join(", ", gacha.RateUpServants)}, starting in {(gacha.StartTime - utcNow).ToNiceString()}");
                                    }
                                }
                            }
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

                var futureEvents = await _service.Config.GetFutureEventsAsync().ConfigureAwait(false);
                if (futureEvents.Any())
                {
                    sb.Append("**Upcoming Event(s):** ");
                    foreach (var ev in futureEvents)
                    {
                        if (ev.StartTime.HasValue)
                        {
                            sb.AppendLine($"(\uFF03{ev.Id}) {ev.EventName}, planned to start in {(ev.StartTime.Value - utcNow).ToNiceString()}.");
                        }
                        else
                        {
                            sb.AppendLine($"(\uFF03{ev.Id}) {ev.EventName}, planned to start at an unknown time.");
                        }

                        if (!String.IsNullOrEmpty(ev.InfoLink))
                        {
                            sb.AppendLine($"<{ev.InfoLink}>");
                        }
                    }
                }
                else
                {
                    sb.AppendLine("No known upcoming events.");
                }
                sb.Append("KanColle Collab never ever");
                await ReplyAsync(sb.ToString()).ConfigureAwait(false);
            }

            [Command("addevent"), Permission(MinimumPermission.ModRole)]
            public async Task AddEventCmd(string name, LocalDateTime? start = null, LocalDateTime? end = null, string? info = null)
            {
                var dtoStart = start?.InZoneLeniently(NodaTimeExtensions.JpnTimeZone).ToDateTimeOffset();
                var dtoEnd = end?.InZoneLeniently(NodaTimeExtensions.JpnTimeZone).ToDateTimeOffset();
                var ev = await _service.Config.AddEventAsync(name, dtoStart, dtoEnd, info).ConfigureAwait(false);
                await ReplyAsync($"Successfully added event \uFF03{ev.Id} **{ev.EventName}**.").ConfigureAwait(false);
            }
        }
    }
}
