using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Addons.SimplePermissions;
using Discord.Commands;

namespace DivaBot
{
    public class ScoreAttackService
    {
        private const ulong _modChannel = 268809118465261568ul;

        private readonly DiscordSocketClient _client;
        private readonly IConfigStore<DivaBotConfig> _store;

        internal ScoreAttackService(
            IConfigStore<DivaBotConfig> store,
            DiscordSocketClient client)
        {
            _client = client;
            _store = store;

            using (var config = store.Load())
            {
                foreach (var sac in config.CurrentChallenges)
                {
                    SetTimer(config, sac.Key, sac.Value);
                }
            }

            //_timer = new Timer(async o =>
            //{
            //    var c = _store.Load();
            //    var submissions = GetSubmissions(c.CurrentChallenge.Scores);
            //    await (_client.GetChannel(FTCompChannel) as IMessageChannel).SendMessageAsync($"The Score Attack challenge for **{c.CurrentChallenge.Title}** is over.");
            //    c.CurrentChallenge = null;
            //    _store.Save();
            //    await (_client.GetChannel(ModChannel) as IMessageChannel).SendMessageAsync($"All the score attack submissions:\n{String.Join("\n", submissions)}");
            //}, null, Timeout.Infinite, Timeout.Infinite);

            //var cc = _store.Load().CurrentChallenge;
            //if (cc != null)
            //    _timer.Change((cc.ExpiresOn - DateTime.UtcNow), Timeout.InfiniteTimeSpan);
        }

        internal ScoreAttackChallenge GetCurrent(ulong key)
        {
            using (var config = _store.Load())
            {
                return config.CurrentChallenges.TryGetValue(key, out var ret)
                           ? ret : null;
            }
        }

        private IEnumerable<string> GetSubmissions(ScoreAttackChallenge sac)
        {
            var srv = _client.GetGuild(268784641874329600ul);

            foreach (var s in sac.Scores["Hard"])
            {
                var user = srv.GetUser(s.Key);
                yield return $"Hard: **{user.Nickname ?? user.Username}**: <{s.Value}>";
            }

            foreach (var s in sac.Scores["Extreme"])
            {
                var user = srv.GetUser(s.Key);
                yield return $"Extreme: **{user.Nickname ?? user.Username}**: <{s.Value}>";
            }

            foreach (var s in sac.Scores["Ex-Extreme"])
            {
                var user = srv.GetUser(s.Key);
                yield return $"Ex-Extreme: **{user.Nickname ?? user.Username}**: <{s.Value}>";
            }
        }

        internal void AddScore(ICommandContext ctx, string diff, string link)
        {
            using (var config = _store.Load())
            {
                config.CurrentChallenges[ctx.Channel.Id].Scores[diff][ctx.User.Id] = link;
            }
            //_store.Save();
        }

        internal void ReplaceChallenge(ulong channel, ScoreAttackChallenge challenge)
        {
            using (var config = _store.Load())
            {
                SetTimer(config, channel, challenge);
                config.CurrentChallenges[channel] = challenge;
            }
            //_store.Save();
        }

        private void SetTimer(DivaBotConfig config, ulong channel, ScoreAttackChallenge challenge)
        {
            challenge._timer = new Timer(async o =>
            {
                var submissions = GetSubmissions(challenge).ToList();
                var challengeChannel = _client.GetChannel(channel) as IMessageChannel;
                await challengeChannel.SendMessageAsync("The Score Attack challenges this week are over.").ConfigureAwait(false);
                config.CurrentChallenges.Remove(channel);
                config.Save();
                await (_client.GetChannel(_modChannel) as IMessageChannel).SendMessageAsync($"Score attack submissions for {challengeChannel.Name}:\n{String.Join("\n", submissions)}").ConfigureAwait(false);
            }, null, challenge.ExpiresOn - DateTime.UtcNow, Timeout.InfiniteTimeSpan);
        }

        //internal ScoreAttackChallenge GetCurrent()
        //    => _store.Load().CurrentChallenge;

        //void IModule.Install(ModuleManager manager)
        //{
        //    var cmds = manager.Client.GetService<CommandService>();

        //    cmds.CreateCommand("newscoreattack")
        //        .Description("Set a new Score Attack Challenge. Sets to expire in one week.")
        //        .AddCheck((cmd, u, ch) => ch.Id == FTCompChannel)
        //        .AddCheck((cmd, u, ch) => u.Roles.Any(r => r.Id == _config.ModGruop))
        //        .Parameter("title", ParameterType.Required)
        //        .Parameter("difficulty", ParameterType.Optional)
        //        .Do(e =>
        //        {
        //            var sac = new ScoreAttackChallenge
        //            {
        //                Title = e.GetArg(0),
        //                Difficulty = e.GetArg(1) != null ? $"({e.GetArg(1)})" : String.Empty,
        //                ExpiresOn = DateTime.UtcNow.AddDays(7),
        //                Scores = new Dictionary<ulong, string>()
        //            };
        //            _config.CurrentChallenge = sac;
        //            _config.Save();
        //            _timer.Change((sac.ExpiresOn - DateTime.UtcNow), Timeout.InfiniteTimeSpan);
        //            return e.Channel.SendMessage($"Set new challenge: **{sac.Title}**");
        //        });

        //    cmds.CreateCommand("scoreattack")
        //        .Description("Show the current Score Attack Challenge.")
        //        .AddCheck((cmd, u, ch) => ch.Id == FTCompChannel)
        //        .Do(e =>
        //        {
        //            if (_config.CurrentChallenge == null)
        //                return e.Channel.SendMessage("No challenge currently active");

        //            var eta = _config.CurrentChallenge.ExpiresOn - DateTime.UtcNow;
        //            var d = eta.Days == 1 ? "day" : "days";
        //            var h = eta.Hours == 1 ? "hour" : "hours";
        //            var m = eta.Minutes == 1 ? "minute" : "minutes";
        //            var etastr = eta.TotalHours < 24 ? $"{eta.Hours} {h} and {eta.Minutes} {m}" : $"{eta.Days} {d} and {eta.Hours} {h}";
        //            return e.Channel.SendMessage($"Current challenge: **{_config.CurrentChallenge.Title}** {_config.CurrentChallenge.Difficulty}\nCloses in {etastr}");
        //        });

        //    cmds.CreateCommand("enterscore")
        //        .Description("Enter a score into the Score Attack Challenge.")
        //        .AddCheck((cmd, u, ch) => ch.Id == FTCompChannel)
        //        .Parameter("img", ParameterType.Optional)
        //        .Do(async e =>
        //        {
        //            await Task.Delay(1000);
        //            string img;
        //            if (e.GetArg(0) == null)
        //            {
        //                if (e.Message.Attachments.Any())
        //                    img = e.Message.Attachments.First().Filename;
        //                else
        //                {
        //                    await e.Channel.SendMessage("No image attached");
        //                    return;
        //                }
        //            }
        //            else
        //            {
        //                if (e.Args[0].StartsWith("http") && (e.Args[0].EndsWith(".jpg") || e.Args[0].EndsWith(".png")))
        //                {
        //                    img = e.Args[0];
        //                }
        //                else
        //                {
        //                    await e.Channel.SendMessage("Argument is not a link or image.");
        //                    return;
        //                }
        //            }

        //            _config.CurrentChallenge.Scores[e.User.Id] = img;
        //            _config.Save();
        //            await e.Channel.SendMessage("Score recorded.");
        //        });

        //    cmds.CreateCommand("myscore")
        //        .Description("See your current score recorded for the Score Attack Challenge.")
        //        .AddCheck((cmd, u, ch) => ch.Id == FTCompChannel)
        //        .Do(async e =>
        //        {
        //            if (_config.CurrentChallenge.Scores.TryGetValue(e.User.Id, out var score))
        //            {
        //                await e.Channel.SendMessage($"Your current score: <{score}>");
        //            }
        //            else
        //            {
        //                await e.Channel.SendMessage("No score currently recorded.");
        //            }
        //        });
        //}
    }

    internal static class Ext
    {
        public static Task AddScoreAttack(
            this CommandService cmds,
            IServiceCollection map,
            IConfigStore<DivaBotConfig> store,
            DiscordSocketClient client)
        {
            map.AddSingleton(new ScoreAttackService(store, client));
            cmds.AddTypeReader<ScoreChallenge>(new ScoreChallengeTypeReader());
            //cmds.AddTypeReader<IEnumerable<IAttachment>>(new AttachmentTypeReader(client));
            return cmds.AddModuleAsync<ScoreAttackModule>();
        }

        public static Task AddTagResponses(
            this CommandService cmds,
            IServiceCollection map,
            IConfigStore<DivaBotConfig> store,
            DiscordSocketClient client)
        {
            using (var config = store.Load())
            {
                map.AddSingleton(new ResponderService(client, config.AutoResponses));
            }
            map.AddSingleton(new TagService(store));
            return cmds.AddModuleAsync<TagModule>();
        }

        //internal static EmbedBuilder AddFieldSequence<T>(this EmbedBuilder builder, IEnumerable<T> seq, Action<EmbedFieldBuilder, T> action)
        //{
        //    foreach (var item in seq)
        //    {
        //        builder.AddField(efb => action(efb, item));
        //    }

        //    return builder;
        //}
    }
}
