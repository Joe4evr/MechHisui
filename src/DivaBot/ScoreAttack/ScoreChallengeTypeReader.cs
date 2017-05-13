using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;

namespace DivaBot
{
    public class ScoreChallengeTypeReader : TypeReader
    {
        private static readonly Regex magic = new Regex(@"(hard:(\s*)""(?<h1>.*?)""((\s*)""(?<h2>.*?)"")?(\s*)extreme:(\s*)""(?<e1>.*?)""((\s*)""(?<e2>.*?)"")?((\s*)ex(-?)ex:(\s*)""(?<ee1>.*?)""((\s*)""(?<ee2>.*?)"")?)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public override Task<TypeReaderResult> Read(ICommandContext context, string input)
        {
            var match = magic.Match(input);
            if (match.Success)
            {
                var sc = new ScoreChallenge
                {
                    HardEN = match.Groups["h1"].Value,
                    HardJP = match.Groups["h2"].Value,
                    ExEN = match.Groups["e1"].Value,
                    ExJP = match.Groups["e2"].Value,
                    ExExEN = match.Groups["ee1"].Value,
                    ExExJP = match.Groups["ee2"].Value
                };
                return Task.FromResult(TypeReaderResult.FromSuccess(sc));
            }

            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Could not parse input according to pattern."));
        }
    }

    public class ScoreChallenge
    {
        public string HardEN { get; set; }
        public string HardJP { get; set; }
        public string ExEN { get; set; }
        public string ExJP { get; set; }
        public string ExExEN { get; set; }
        public string ExExJP { get; set; }
    }
}
