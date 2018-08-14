using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Discord.Addons.SimplePermissions;
using MechHisui.SecretHitler;

namespace MechHisui.Core
{
    public sealed class SecretHitlerConfig : ISecretHitlerConfig
    {
        private readonly IConfigStore<MechHisuiConfig> _store;
        private readonly IServiceProvider _services;

        public SecretHitlerConfig(IConfigStore<MechHisuiConfig> store, IServiceProvider services)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }


        async Task<IEnumerable<string>> ISecretHitlerConfig.GetThemeKeysAsync()
        {
            using (var config = _store.Load(_services))
            {
                return await config.SHThemes
                    .AsNoTracking()
                    .Select(t => t.Key)
                    .ToListAsync();
            }
        }

        async Task<ISecretHitlerTheme> ISecretHitlerConfig.GetThemeAsync(string key)
        {
            using (var config = _store.Load(_services))
            {
                return await config.SHThemes
                    .AsNoTracking()
                    .SingleOrDefaultAsync(t => t.Key == key);
            }
        }
    }
}
