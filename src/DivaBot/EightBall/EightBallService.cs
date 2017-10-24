using System;
using System.Collections.Generic;
using System.Linq;
using SharedExtensions.Collections;

namespace DivaBot
{
    public class EightBallService
    {
        internal HashSet<string> Options { get; }
        internal Random Rng { get; } = new Random();

        public EightBallService(IEnumerable<string> additionalOptions)
        {
            var o = new List<string>
            {
                "Yes",
                "No",
                "It is certain",
                "It is decidedly so",
                "Without a doubt",
                "Yes, definitely",
                "You may rely on it",
                "As I see it, yes",
                "Most likely",
                "Outlook good",
                "Signs point to yes",
                "Reply hazy try again",
                "Ask again later",
                "Better not tell you now",
                "Cannot predict now",
                "Concentrate and ask again",
                "Don't count on it",
                "My reply is no",
                "My sources say no",
                "Outlook not so good",
                "Very doubtful"
            };
            //Options = new HashSet<string>(o.Concat(additionalOptions ?? Enumerable.Empty<string>()).Shuffle(28));
        }
    }
}
