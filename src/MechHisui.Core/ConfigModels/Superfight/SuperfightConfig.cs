using System;
using System.Collections.Generic;
using System.Linq;
using Discord.Addons.SimplePermissions;
using MechHisui.Superfight;

namespace MechHisui.Core
{
    public sealed class SuperfightConfig : ISuperfightConfig
    {
        private readonly IConfigStore<MechHisuiConfig> _store;

        public SuperfightConfig(IConfigStore<MechHisuiConfig> store)
        {
            _store = store;
        }

        public IEnumerable<string> GetCharacters()
        {
            using (var config = _store.Load())
            {
                return config.SFCards.Where(c => c.CardType == CardType.Character)
                    .Select(c => c.Value);
            }
        }

        public IEnumerable<string> GetAbilities()
        {
            using (var config = _store.Load())
            {
                return config.SFCards.Where(c => c.CardType == CardType.Ability)
                    .Select(c => c.Value);
            }
        }

        public IEnumerable<string> GetLocations()
        {
            using (var config = _store.Load())
            {
                return config.SFCards.Where(c => c.CardType == CardType.Location)
                    .Select(c => c.Value);
            }
        }
    }
}
