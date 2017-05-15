using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Addons.MpGame;
using Discord.Commands;
using MechHisui.SecretHitler.Models;
using System.Linq;

namespace MechHisui.SecretHitler
{
    public sealed class SecretHitlerService : MpGameService<SecretHitlerGame, SecretHitlerPlayer>
    {
        internal readonly ConcurrentDictionary<IMessageChannel, HouseRules> HouseRulesList
            = new ConcurrentDictionary<IMessageChannel, HouseRules>(MessageChannelComparer);

        internal readonly IReadOnlyDictionary<string, SecretHitlerConfig> Configs;

        public SecretHitlerService(Dictionary<string, SecretHitlerConfig> configs)
        {
            Configs = configs;
        }
    }

    public static class SHExtensions
    {
        public static Task AddSecretHitler(
            this CommandService cmds,
            IServiceCollection map,
            IEnumerable<SecretHitlerConfig> configs)
        {
            map.AddSingleton(new SecretHitlerService(configs.ToDictionary(keySelector: shc => shc.Key)));
            return cmds.AddModuleAsync<SecretHitlerModule>();
        }
    }
}
