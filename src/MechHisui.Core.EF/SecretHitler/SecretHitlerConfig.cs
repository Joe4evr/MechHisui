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

        public SecretHitlerConfig(IConfigStore<MechHisuiConfig> store)
        {
            _store = store;
        }


        async Task<IEnumerable<string>> ISecretHitlerConfig.GetThemeKeysAsync()
        {
            using (var config = _store.Load())
            {
                return await config.SHThemes
                    .AsNoTracking()
                    .Select(t => t.Key)
                    .ToListAsync();
            }
        }

        async Task<ISecretHitlerTheme> ISecretHitlerConfig.GetThemeAsync(string key)
        {
            using (var config = _store.Load())
            {
                return await config.SHThemes
                    .AsNoTracking()
                    .SingleOrDefaultAsync(t => t.Key == key);
            }
        }
    }
}
