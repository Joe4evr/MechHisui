using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using MechHisui;

namespace MechHisui.Modules
{
    public class Responder
    {
        public Channel channel { get; }

        private ConcurrentDictionary<string, DateTime> _lastResponses = new ConcurrentDictionary<string, DateTime>();

        public Responder(Channel channel, DiscordClient client)
        {
            this.channel = channel;
            client.GetResponders().Add(this);
        }

        internal void ResetTimeouts() => _lastResponses = new ConcurrentDictionary<string, DateTime>();

        internal async void Respond(object sender, MessageEventArgs e)
        {
            if (e.Channel == channel)
            {
                string response = String.Empty;
                var key = Responses.responseDict.Keys.Where(k => k.Contains(e.Message.Text.ToLowerInvariant().Trim())).SingleOrDefault();
                var sKey = Responses.spammableResponses.Keys.Where(k => k.Contains(e.Message.Text.Trim())).SingleOrDefault();

                if (key != null && Responses.responseDict.TryGetValue(key, out response) && response != String.Empty)
                {
                    DateTime last;
                    var msgTime = e.Message.Timestamp.ToUniversalTime();
                    if (!_lastResponses.TryGetValue(response, out last) || (DateTime.UtcNow - last) > TimeSpan.FromMinutes(1))
                    {
                        _lastResponses.AddOrUpdate(response, msgTime, (k, v) => v = msgTime);
                        await ((DiscordClient)sender).SendMessage(e.Channel, response);
                    }
                }
                else if (sKey != null && Responses.spammableResponses.TryGetValue(sKey, out response))
                {
                    await ((DiscordClient)sender).SendMessage(e.Channel, response);
                }
            }
        }
    }

    internal static class Responses
    {
        internal static IReadOnlyDictionary<string[], string> responseDict = new Dictionary<string[], string>()
        {
            { new[] { "osu", "hi" }, "Greetings." },
            { new[] { "bye" }, "Take care." },
            { new[] { "back", "i'm back" }, "Welcome back, master." },
            { new[] { "make me a sandwich" }, "Make one yourself." },
            { new[] { "sudo make me a sandwich" }, "Insufficient privilege." },
            //{ new[] { "", "" }, "" },
            //{ new[] { "", "" }, "" },
            //{ new[] { "", "" }, "" },
            //{ new[] { "", "" }, "" },
        };

        internal static IReadOnlyDictionary<string[], string> spammableResponses = new Dictionary<string[], string>()
        {
            { new[] { "(╯°□°）╯︵ ┻━┻", "(ノಠ益ಠ)ノ彡┻━┻", "(╯°□°）╯ ︵ ┻━┻", "(╯°□°）╯ ︵ ┻━┻"  }, "┬─┬ノ( º _ ºノ)" },
            { new[] { "┻━┻ ︵ヽ(`Д´)ﾉ︵﻿ ┻━┻", "┻━┻︵ (°□°)/ ︵ ┻━┻" }, "┬─┬ノ( º _ ºノ)\n(ヽº _ º )ヽ┬─┬" },
            //{ new[] { "", "" }, "" },
            //{ new[] { "", "" }, "" },
        };
    }
}
