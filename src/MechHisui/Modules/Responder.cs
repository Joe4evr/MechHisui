using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Discord;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace MechHisui.Modules
{
    public class Responder
    {
        public Channel channel { get; }

        private ConcurrentDictionary<Response, DateTime> _lastResponses = new ConcurrentDictionary<Response, DateTime>();

        public Responder(Channel channel, DiscordClient client)
        {
            this.channel = channel;
            client.GetResponders().Add(this);
        }

        internal void ResetTimeouts() => _lastResponses = new ConcurrentDictionary<Response, DateTime>();

        internal async void Respond(object sender, MessageEventArgs e)
        {
            if (e.Channel.Id == channel.Id)
            {
                string temp = (e.Message.Text.StartsWith("@") ? new string(e.Message.Text.SkipWhile(c => Char.IsWhiteSpace(c)).ToArray()) : e.Message.Text);
 
                string quickResponse = String.Empty;
                Func<Response, bool> pred = (k => k.Call.Contains(temp.ToLowerInvariant().Trim()));
                var resp = Responses.responseDict.SingleOrDefault(pred);
                var sResp = Responses.spammableResponses.SingleOrDefault(pred);

                if (resp != null)
                {
                    DateTime last;
                    var msgTime = e.Message.Timestamp.ToUniversalTime();
                    if (!_lastResponses.TryGetValue(resp, out last) || (DateTime.UtcNow - last) > TimeSpan.FromMinutes(1))
                    {
                        _lastResponses.AddOrUpdate(resp, msgTime, (k, v) => v = msgTime);
                        await ((DiscordClient)sender).SendMessage(e.Channel, resp.Resp[new Random().Next() % resp.Resp.Length]);
                    }
                }
                else if (sResp != null)
                {
                    await ((DiscordClient)sender).SendMessage(e.Channel, sResp.Resp[new Random().Next() % sResp.Resp.Length]);
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
                responseDict = JsonConvert.DeserializeObject<List<Response>>(tr.ReadToEnd()) ?? new List<Response>();
            }
            using (TextReader tr = new StreamReader(config["SpamResponsesPath"]))
            {
                spammableResponses = JsonConvert.DeserializeObject<List<Response>>(tr.ReadToEnd()) ?? new List<Response>();
            }
        }

        internal static List<Response> responseDict;

        internal static List<Response> spammableResponses;
    }
}
