﻿using System;
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
        internal ConcurrentDictionary<IMessageChannel, HouseRules> HouseRulesList { get; }
            = new ConcurrentDictionary<IMessageChannel, HouseRules>(MessageChannelComparer);

        private ConcurrentDictionary<string, ISecretHitlerTheme> CachedThemes { get; }
            = new ConcurrentDictionary<string, ISecretHitlerTheme>(StringComparer.OrdinalIgnoreCase);

        private readonly ISecretHitlerConfig _config;

        public SecretHitlerService(
            BaseSocketClient client,
            ISecretHitlerConfig shconfig,
            //IMpGameServiceConfig? mpconfig = null,
            Func<LogMessage, Task>? logger = null)
            : base(client, shconfig, logger)
        {
            _config = shconfig;
        }

        internal async Task<ISecretHitlerTheme?> GetThemeAsync(string key)
        {
            if (!CachedThemes.TryGetValue(key, out var theme))
            {
                theme = await _config.GetThemeAsync(key).ConfigureAwait(false);
                if (theme != null)
                {
                    CachedThemes.TryAdd(key, theme);
                }
            }
            return theme;
        }

        internal Task<IEnumerable<string>> GetThemeKeysAsync()
            => _config.GetThemeKeysAsync();
    }

    //public static class SHExtensions
    //{
    //    public static Task AddSecretHitler(
    //        this CommandService cmds,
    //        DiscordSocketClient client,
    //        IServiceCollection map,
    //        IEnumerable<SecretHitlerConfig> configs,
    //        Func<LogMessage, Task> logger = null)
    //    {
    //        map.AddSingleton(new SecretHitlerService(configs.ToDictionary(keySelector: shc => shc.Key), client, logger)
    //            //.AddPlayerTypereader<SecretHitlerService,SecretHitlerGame, SecretHitlerPlayer>(cmds)
    //            );
            
    //        return cmds.AddModuleAsync<SecretHitlerModule>();
    //    }
    //}
}
