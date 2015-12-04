using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Commands;
using Discord.Modules;
using MechHisui.Modules;


namespace MechHisui.Commands
{
    public static class RegisterCommands
    {
        public static void RegisterResetCommand(this DiscordClient client, IConfiguration config)
        {
            client.Commands().CreateCommand("reset")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && Program.IsWhilested(ch, client))
                .Hide()
                .Do(async cea =>
                {
                    var resp = client.GetResponders().Where(r => r.channel.Id == cea.Channel.Id).Single();
                    resp.ResetTimeouts();
                    await client.SendMessage(cea.Channel, "Timeouts reset.");
                });
        }

        public static void RegisterRecording(this DiscordClient client, IConfiguration config)
        {
            client.Commands().CreateCommand("record")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && Program.IsWhilested(ch, client))
                .Hide()
                .Do(async cea =>
                {
                    var rec = client.GetRecordingChannels();
                    if (rec.Contains(cea.Channel.Id))
                    {
                        await client.SendMessage(cea.Channel, $"Already recording here.");
                    }
                    else
                    {
                        rec.Add(cea.Channel.Id);
                        var recorder = new Recorder(cea.Channel, client);
                        client.GetRecorders().Add(recorder);
                    }
                });

            client.Commands().CreateCommand("endrecord")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && Program.IsWhilested(ch, client))
                .Hide()
                .Do(async cea =>
                {
                    var rec = client.GetRecordingChannels();
                    if (!rec.Contains(cea.Channel.Id))
                    {
                        await client.SendMessage(cea.Channel, $"Not recording here.");
                    }
                    else
                    {
                        var recorder = client.GetRecorders().Where(r => r.channel == cea.Channel).Single();
                        await recorder.EndRecord(client);
                        client.GetRecorders().Remove(recorder);
                        rec.Remove(cea.Channel.Id);
                    }
                });
        }

        public static void RegisterDisconnectCommand(this DiscordClient client, IConfiguration config)
        {
            client.Commands().CreateCommand("disconnect")
                .AddCheck((c, u, ch) => u.Id == Int64.Parse(config["Owner"]) && Program.IsWhilested(ch, client))
                .Hide()
                .Do(async cea =>
                {
                    var recs = client.GetRecorders();
                    if (recs.Any())
                    {
                        foreach (var rec in recs)
                        {
                            await rec.EndRecord(client);
                        }
                    }

                    var resps = client.GetResponders();
                    if (resps.Any())
                    {
                        foreach (var resp in resps)
                        {
                            client.MessageReceived -= resp.Respond;
                        }
                    }

                    foreach (var ch in client.Modules().Modules
                        .Where(m => m.Id == nameof(ChannelWhitelistModule))
                        .Single()
                        .EnabledChannels)
                    {
                        await client.SendMessage(ch, "Mech-Hisui shutting down.");
                    }
                });
        }
    }
}
