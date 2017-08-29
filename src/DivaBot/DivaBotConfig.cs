using System;
using System.Collections.Generic;
#if !ARM
using Discord.Addons.SimpleAudio;
#endif
using Discord.Addons.SimplePermissions;

namespace DivaBot
{
    internal class DivaBotConfig : JsonConfigBase
    {
        public string LoginToken { get; set; }

#if !ARM
        public AudioConfig AudioConfig { get; set; }
#endif

        public Dictionary<string, string[]> AutoResponses { get; set; }

        public Dictionary<string, string> TagResponses { get; set; }

        public Dictionary<ulong, ScoreAttackChallenge> CurrentChallenges { get; set; }

        public List<string> Additional8BallOptions { get; set; }
    }
}