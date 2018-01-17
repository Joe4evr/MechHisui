using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.MpGame;
using MechHisui.SecretHitler.Models;

namespace MechHisui.SecretHitler
{
    public sealed class SecretHitlerService : MpGameService<SecretHitlerGame, SecretHitlerPlayer>
    {
        internal readonly ConcurrentDictionary<IMessageChannel, HouseRules> HouseRulesList
            = new ConcurrentDictionary<IMessageChannel, HouseRules>(MessageChannelComparer);

        internal readonly IReadOnlyDictionary<string, SecretHitlerConfig> Configs;

        public SecretHitlerService(IReadOnlyDictionary<string, SecretHitlerConfig> configs,
            DiscordSocketClient client,
            Func<LogMessage, Task> logger = null)
            : base(client, logger)
        {
            Configs = configs;
        }
    }

    public static class SHExtensions
    {
        public static Task AddSecretHitler(
            this CommandService cmds,
            DiscordSocketClient client,
            IServiceCollection map,
            IEnumerable<SecretHitlerConfig> configs,
            Func<LogMessage, Task> logger = null)
        {
            map.AddSingleton(new SecretHitlerService(configs.ToDictionary(keySelector: shc => shc.Key), client, logger)
                //.AddPlayerTypereader<SecretHitlerService,SecretHitlerGame, SecretHitlerPlayer>(cmds)
                );
            
            return cmds.AddModuleAsync<SecretHitlerModule>();
        }
    }
}
