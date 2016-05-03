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
        private ConcurrentDictionary<string[], DateTime> _lastResponses = new ConcurrentDictionary<string[], DateTime>();
        
        internal void ResetTimeouts() => _lastResponses = new ConcurrentDictionary<string[], DateTime>();

        internal async void Respond(object sender, MessageEventArgs e)
        {
            string temp = (e?.Message?.Text?.StartsWith("@") ?? false
                ? new string(e.Message.Text.SkipWhile(c => !Char.IsWhiteSpace(c)).ToArray())
                : e.Message.Text);

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
                        await e.Channel.SendMessage(resp.Value[new Random().Next(maxValue: resp.Value.Length)]);
                    }
                }
                else if (sResp != null)
                {
                    await e.Channel.SendMessage(sResp.Resp[new Random().Next(maxValue: sResp.Resp.Length)]);
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
            var temp = JsonConvert.DeserializeObject<List<Response>>(File.ReadAllText(config["ResponsesPath"])) ?? new List<Response>();
            foreach (var item in temp)
            {
                responseDict.AddOrUpdate(item.Call, item.Resp, (k, v) => v = item.Resp);
            }
            
            spammableResponses = JsonConvert.DeserializeObject<List<Response>>(File.ReadAllText(config["SpamResponsesPath"])) ?? new List<Response>();
        }

        internal static ConcurrentDictionary<string[], string[]> responseDict = new ConcurrentDictionary<string[], string[]>();

        internal static List<Response> spammableResponses;
    }
}
