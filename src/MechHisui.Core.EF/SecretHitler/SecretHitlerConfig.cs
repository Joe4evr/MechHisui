using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Discord.Addons.MpGame;
using Discord.Addons.SimplePermissions;
using MechHisui.SecretHitler;

namespace MechHisui.Core
{
    public sealed class SecretHitlerConfig : ISecretHitlerConfig
    {
        private readonly IConfigStore<MechHisuiConfig> _store;
        private readonly IMpGameServiceConfig _baseConfig;

        public SecretHitlerConfig(
            IConfigStore<MechHisuiConfig> store, IMpGameServiceConfig? baseConfig = null)
        {
            _store = store;
            _baseConfig = baseConfig ?? IMpGameServiceConfig.Default;
        }


        async Task<IEnumerable<string>> ISecretHitlerConfig.GetThemeKeysAsync()
        {
            using var config = _store.Load();
            return await config.SHThemes
                .AsNoTracking()
                .Select(t => t.Key)
                .ToListAsync();
        }

        async Task<ISecretHitlerTheme?> ISecretHitlerConfig.GetThemeAsync(string key)
        {
            using var config = _store.Load();
            return await config.SHThemes
                .AsNoTracking()
                .SingleOrDefaultAsync(t => t.Key == key);
        }

        ILogStrings IMpGameServiceConfig.LogStrings => _baseConfig.LogStrings ?? ILogStrings.Default;

        bool IMpGameServiceConfig.AllowJoinMidGame => false;

        bool IMpGameServiceConfig.AllowLeaveMidGame => false;
    }
}
