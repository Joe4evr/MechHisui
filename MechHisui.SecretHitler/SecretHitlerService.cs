using System.Collections.Concurrent;
using System.Collections.Generic;
using Discord.Addons.MpGame;
using MechHisui.SecretHitler.Models;

namespace MechHisui.SecretHitler
{
    public sealed class SecretHitlerService : MpGameService<SecretHitlerGame, SecretHitlerPlayer>
    {
        internal readonly ConcurrentDictionary<ulong, HouseRules> HouseRulesList
            = new ConcurrentDictionary<ulong, HouseRules>();

        internal readonly IReadOnlyDictionary<string, SecretHitlerConfig> Configs;

        public SecretHitlerService(Dictionary<string, SecretHitlerConfig> configs)
        {
            Configs = configs;
        }
    }
}
