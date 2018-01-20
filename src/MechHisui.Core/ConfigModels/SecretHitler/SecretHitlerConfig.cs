using System;
using System.Collections.Generic;
using System.Linq;
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

        public IEnumerable<string> GetKeys()
        {
            using (var config = _store.Load())
            {
                return config.SHThemes.Select(t => t.Key).ToList();
            }
        }

        public ISecretHitlerTheme GetTheme(string key)
        {
            using (var config = _store.Load())
            {
                return config.SHThemes.SingleOrDefault(t => t.Key == key);
            }
        }
    }
}
