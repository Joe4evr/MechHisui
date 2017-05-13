using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Addons.SimplePermissions;

namespace DivaBot
{
    [Name("ScoreAttack")]
    public sealed class ScoreAttackModule : ModuleBase<ICommandContext>
    {
        private readonly ScoreAttackService _service;

        public ScoreAttackModule(ScoreAttackService service)
        {
            _service = service;
        }

        [Command("newscoreattack"), Permission(MinimumPermission.ModRole)]
        [Summary("Set a new Score Attack Challenge. Sets to expire in one week.")]
        public Task NewSCCmd([Remainder] ScoreChallenge sc)
        {
            var sac = new ScoreAttackChallenge
            {
                Titles = new Dictionary<string, string>
                {
                    ["Hard"] = $"{sc.HardEN} {(sc.HardJP != null ? $"({sc.HardJP})" : String.Empty)}",
                    ["Extreme"] = $"{sc.ExEN} {(sc.ExJP != null ? $"({sc.ExJP})" : String.Empty)}",
                    ["Ex-Extreme"] = $"{sc.ExExEN} {(sc.ExExJP != null ? $"({sc.ExExJP})" : String.Empty)}"
                },
                ExpiresOn = DateTime.UtcNow.AddDays(7),
                Scores = new Dictionary<string, Dictionary<ulong, string>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Hard"] = new Dictionary<ulong, string>(),
                    ["Extreme"] = new Dictionary<ulong, string>(),
                    ["Ex-Extreme"] = new Dictionary<ulong, string>()
                }
            };

            _service.ReplaceChallenge(Context.Channel.Id, sac);
            return (!String.IsNullOrEmpty(sc.ExExEN))
                ? ReplyAsync($"Set new challenges: **{String.Join("**, **", sc.HardEN, sc.ExEN, sc.ExExEN)}**")
                : ReplyAsync($"Set new challenges: **{String.Join("**, **", sc.HardEN, sc.ExEN)}**");
        }

        [Command("scoreattack"), Permission(MinimumPermission.Everyone)]
        [Summary("Show the current Score Attack Challenge.")]
        public Task CurrentSCCmd()
        {
            var current = _service.GetCurrent(Context.Channel.Id);
            if (current == null)
                return ReplyAsync("No challenges currently active");

            var eta = current.ExpiresOn - DateTime.UtcNow;
            var d = eta.Days == 1 ? "day" : "days";
            var h = eta.Hours == 1 ? "hour" : "hours";
            var m = eta.Minutes == 1 ? "minute" : "minutes";
            var etastr = eta.TotalHours < 24 ? $"{eta.Hours} {h} and {eta.Minutes} {m}" : $"{eta.Days} {d} and {eta.Hours} {h}";

            return ReplyAsync($"Current challenges: {String.Join("\n", current.Titles.Select(kv => $"{kv.Key}: **{kv.Value}**"))}\nCloses in {etastr}");
        }

        [Command("enterscore"), Permission(MinimumPermission.Everyone)]
        [Summary("Enter a score into the Score Attack Challenge.")]
        public Task EnterScoreCmd(string difficulty, string link)
        {
            if (link.StartsWith("http")/* && (link.EndsWith(".jpg") || link.EndsWith(".png"))*/)
            {
                _service.AddScore(Context, difficulty, link);
                return ReplyAsync("Score recorded.");
            }
            else
            {
                return ReplyAsync("Argument is not a link.");
            }
        }

        //[Command("enterscore"), Permission(MinimumPermission.Everyone)]
        //[Summary("Enter a score into the Score Attack Challenge.")]
        //public async Task EnterScoreCmd(IEnumerable<IAttachment> attachment)
        //{
        //    var url = attachment.FirstOrDefault()?.Url;
        //    if (url != null)
        //    {
        //        _service.AddScore(Context, url);
        //        await ReplyAsync("Score recorded.");
        //    }
        //}

        [Command("myscore"), Permission(MinimumPermission.Everyone)]
        [Summary("See your current score recorded for the Score Attack Challenge.")]
        public Task MyScoreCmd()
        {
            var current = _service.GetCurrent(Context.Channel.Id);
            var scores = current.Scores.Select(kv =>
            {
                return (kv.Value.TryGetValue(Context.User.Id, out var sc))
                   ? $"{kv.Key}: <{sc}>"
                   : $"None for {kv.Key}";
            });

            return ReplyAsync($"Your current scores: {String.Join("\n", scores)}");
        }
    }
}
