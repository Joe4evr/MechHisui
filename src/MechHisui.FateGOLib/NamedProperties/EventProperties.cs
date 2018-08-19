using System;
using Discord.Commands;
using NodaTime;

namespace MechHisui.FateGOLib
{
    [NamedArgumentType]
    public sealed class EventProperties
    {
        public ZonedDateTime? Start { get; set; }
        public ZonedDateTime? End { get; set; }
        public string Info { get; set; }
    }
}
