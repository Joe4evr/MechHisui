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

        private ConcurrentDictionary<string[], DateTime> _lastResponses = new ConcurrentDictionary<string[], DateTime>();

        public Responder(Channel channel, DiscordClient client)
        {
            this.channel = channel;
            client.GetResponders().Add(this);
        }

        internal void ResetTimeouts() => _lastResponses = new ConcurrentDictionary<string[], DateTime>();

        internal async void Respond(object sender, MessageEventArgs e)
        {
            if (e.Channel.Id == channel.Id)
            {
                string temp = (e.Message.Text.StartsWith("@") ? new string(e.Message.Text.SkipWhile(c => Char.IsWhiteSpace(c)).ToArray()) : e.Message.Text);
                string[] responses;
                string quickResponse = String.Empty;
                var key = Responses.responseDict.Keys.Where(k => k.Contains(temp.ToLowerInvariant().Trim())).SingleOrDefault();
                var sKey = Responses.spammableResponses.Keys.Where(k => k.Contains(temp.Trim())).SingleOrDefault();

                if (key != null && Responses.responseDict.TryGetValue(key, out responses) && responses != null)
                {
                    DateTime last;
                    var msgTime = e.Message.Timestamp.ToUniversalTime();
                    if (!_lastResponses.TryGetValue(responses, out last) || (DateTime.UtcNow - last) > TimeSpan.FromMinutes(1))
                    {
                        _lastResponses.AddOrUpdate(responses, msgTime, (k, v) => v = msgTime);
                        await ((DiscordClient)sender).SendMessage(e.Channel, responses[new Random().Next() % responses.Length]);
                    }
                }
                else if (sKey != null && Responses.spammableResponses.TryGetValue(sKey, out responses))
                {
                    await ((DiscordClient)sender).SendMessage(e.Channel, responses[new Random().Next() % responses.Length]);
                }
                else if (Responses.quickLearn.TryGetValue(e.Message.Text, out quickResponse) && quickResponse != String.Empty)
                {
                    await ((DiscordClient)sender).SendMessage(e.Channel, quickResponse);
                }
            }
        }
    }

    internal static class Responses
    {
        internal static IReadOnlyDictionary<string[], string[]> responseDict = new Dictionary<string[], string[]>()
        {
            { new[] { "osu", "hi" }, new[] { "Greetings." } },
            { new[] { "bye" }, new[] { "Take care." } },
            { new[] { "back", "i'm back", "tadaima" }, new[] { "Welcome back, master." } },
            { new[] { "make me a sandwich" }, new[] { "Make one yourself." } },
            { new[] { "sudo make me a sandwich" }, new[] { "Insufficient privilege." } },
            { new[] { "good hisui" }, new[] { "*bows* Thank you, master." } },
            //{ new[] { "", "" }, new[] { "" } },
            //{ new[] { "", "" }, new[] { "" } },
        };

        internal static IReadOnlyDictionary<string[], string[]> spammableResponses = new Dictionary<string[], string[]>()
        {
            { new[] { "(╯°□°）╯︵ ┻━┻", "(ノಠ益ಠ)ノ彡┻━┻", "(╯°□°）╯ ︵ ┻━┻", "(╯°□°）╯ ︵ ┻━┻"  }, new[] { "┬─┬ノ( º _ ºノ)" } },
            { new[] { "┻━┻ ︵ヽ(`Д´)ﾉ︵﻿ ┻━┻", "┻━┻︵ (°□°)/ ︵ ┻━┻" }, new[] { "┬─┬ノ( º _ ºノ)\n(ヽº _ º )ヽ┬─┬" } },
            { new[] { "ding dong" }, new[] { "bing bong" } },
            //{ new[] { "", "" }, new[] { "" },
            //{ new[] { "", "" }, new[] { "" },
        };

        internal static ConcurrentDictionary<string, string> quickLearn = new ConcurrentDictionary<string, string>();
    }
}
