using System;
using System.Collections.Generic;
using Discord.WebSocket;

namespace DivaBot
{
    public sealed class ResponderService
    {
        private readonly Random _rng = new Random();
        private readonly Dictionary<string, string[]> _responses;

        internal ResponderService(DiscordSocketClient client, Dictionary<string, string[]> responses)
        {
            _responses = responses;

            client.MessageReceived += async msg =>
            {
                string mention = client.CurrentUser.Mention;
                if (msg.Content.StartsWith(mention))
                {
                    var call = msg.Content.Substring(mention.Length);

                    if (_responses.ContainsKey(call))
                    {
                        string[] resps = _responses[call];
                        await msg.Channel.SendMessageAsync(resps[_rng.Next(maxValue: resps.Length)]).ConfigureAwait(false);
                    }
                }
            };
        }
    }
}
