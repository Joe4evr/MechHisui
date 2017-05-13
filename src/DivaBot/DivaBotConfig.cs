using System;
using System.Collections.Generic;
using Discord.Addons.SimpleAudio;
using Discord.Addons.SimplePermissions;

namespace DivaBot
{
    internal class DivaBotConfig : JsonConfigBase
    {
        public string LoginToken { get; set; }

        public AudioConfig AudioConfig { get; set; }

        public Dictionary<string, string[]> AutoResponses { get; set; }

        public Dictionary<string, string> TagResponses { get; set; }

        public Dictionary<ulong, ScoreAttackChallenge> CurrentChallenges { get; set; }

        public List<string> Additional8BallOptions { get; set; }
    }
}