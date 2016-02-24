using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Discord;
using JiiLib;
using Newtonsoft.Json;

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
                string temp = (e?.Message?.Text?.StartsWith("@", StringComparison.InvariantCultureIgnoreCase) ?? false ? new string(e.Message.Text.SkipWhile(c => !Char.IsWhiteSpace(c)).ToArray()) : e.Message.Text);

                if (!String.IsNullOrEmpty(temp))
                {
                    string quickResponse = String.Empty;
                    Func<Response, bool> pred = (k => k?.Call?.ContainsIgnoreCase(temp.Trim()) ?? false);
                    var resp = Responses.responseDict.SingleOrDefault(k => k.Key?.ContainsIgnoreCase(temp.Trim()) ?? false);
                    var sResp = Responses.spammableResponses.SingleOrDefault(pred);

                    if (resp.Key != null)
                    {
                        DateTime last;
                        var msgTime = e.Message.Timestamp.ToUniversalTime();
                        if (!_lastResponses.TryGetValue(resp.Key, out last) || (DateTime.UtcNow - last) > TimeSpan.FromMinutes(1))
                        {
                            _lastResponses.AddOrUpdate(resp.Key, msgTime, (k, v) => v = msgTime);
                            await e.Channel.SendMessage(resp.Value[new Random().Next() % resp.Value.Length]);
                        }
                    }
                    else if (sResp != null)
                    {
                        await e.Channel.SendMessage(sResp.Resp[new Random().Next() % sResp.Resp.Length]);
                    }
                }
            }
        }
    }

    public class Response
    {
        public string[] Call { get; set; }
        public string[] Resp { get; set; }
    }

    internal static class Responses
    {
        public static void InitResponses(IConfiguration config)
        {
            using (TextReader tr = new StreamReader(config["ResponsesPath"]))
            {
                var temp = JsonConvert.DeserializeObject<List<Response>>(tr.ReadToEnd()) ?? new List<Response>();
                foreach (var item in temp)
                {
                    responseDict.AddOrUpdate(item.Call, item.Resp, (k, v) => v = item.Resp);
                }
            }
            using (TextReader tr = new StreamReader(config["SpamResponsesPath"]))
            {
                spammableResponses = JsonConvert.DeserializeObject<List<Response>>(tr.ReadToEnd()) ?? new List<Response>();
            }
        }

        internal static ConcurrentDictionary<string[], string[]> responseDict = new ConcurrentDictionary<string[], string[]>();

        internal static List<Response> spammableResponses;
    }
}
