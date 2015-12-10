using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using MechHisui.Modules;

namespace MechHisui
{
    internal static class ClientExtensions
    {
        private static List<long> _recording = new List<long>();
        internal static List<long> GetRecordingChannels(this DiscordClient client) => _recording;

        private static List<Recorder> _recorders = new List<Recorder>();
        internal static List<Recorder> GetRecorders(this DiscordClient client) => _recorders;
        
        private static List<Responder> _responders = new List<Responder>();
        internal static List<Responder> GetResponders(this DiscordClient client) => _responders;
    }
}
