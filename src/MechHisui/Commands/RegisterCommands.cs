using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Commands;
using Discord.Modules;
using MechHisui.Modules;
using System.Threading;

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

        public static void RegisterWikiCommand(this DiscordClient client, IConfiguration config, Wikier wikier)
        {
            client.Commands().CreateCommand("wiki")
                .AddCheck((c, u, ch) => ch.Id == Int64.Parse(config["FGO_general"]))
                .Parameter("servantname", ParameterType.Required)
                .Description("Relay information on the specified Servant. Alternative names acceptable. Provide names of multiple words inside quotation marks.")
                .Do(async cea =>
                {
                    var arg = cea.GetArg("servantname");
                    if (arg.ToLowerInvariant() == "waifu")
                    {
                        await client.SendMessage(cea.Channel, "It has come to my attention that your 'waifu' is equatable to feces.");
                        return;
                    }
                    
                    if ((new[] { "scath", "scathach" }).Contains(arg))
                    {
                        await client.SendMessage(cea.Channel, "Never ever.");
                        return;
                    }

                    var article = await wikier.LookupStats(arg, new CancellationToken());
                    if (article == null)
                    {
                        await client.SendMessage(cea.Channel, "No such article found. Please try another name.");
                    }
                    else
                    {
                        string response = $"**Servant:** {article.Sections.First().Title}";

                        await client.SendMessage(cea.Channel, response);
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
                        .Where(m => m.Id == nameof(ChannelWhitelistModule).ToLowerInvariant())
                        .Single()
                        .EnabledChannels)
                    {
                        if (ch.Id != Int64.Parse(config["API_testing"]))
                        {
                            await client.SendMessage(ch, config["Goodbye"]);
                        }
                    }

                    Environment.Exit(0);
                });
        }

        public static void RegisterDailyComand(this DiscordClient client, IConfiguration config)
        {
            client.Commands().CreateCommand("daily")
                .AddCheck((c, u, ch) => ch.Id == Int64.Parse(config["FGO_general"]))
                .Parameter("day", ParameterType.Optional)
                .Description("Relay the information of daily quests for the specified day. Default to current day.")
                .Do(async cea =>
                {
                    DayOfWeek day;
                    var arg = cea.GetArg("day")?.ToLowerInvariant();
                    if (String.IsNullOrWhiteSpace(arg))
                    {
                        
                    }
                    else if (Enum.TryParse(arg, out day))
                    {
                        
                    }
                    else
                    {
                        await client.SendMessage(cea.Channel, "Could not convert argument to a day of the week. Please try again.");
                    }
                });
        }
    }
}
