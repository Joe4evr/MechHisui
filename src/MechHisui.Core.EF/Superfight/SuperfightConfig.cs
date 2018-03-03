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

        public IEnumerable<ISuperfightCard> GetCharacters()
        {
            using (var config = _store.Load())
            {
                return config.SFCards.Where(c => c.Type == CardType.Character).ToList();
            }
        }

        public IEnumerable<ISuperfightCard> GetAbilities()
        {
            using (var config = _store.Load())
            {
                return config.SFCards.Where(c => c.Type == CardType.Ability).ToList();
            }
        }

        public IEnumerable<ISuperfightCard> GetLocations()
        {
            using (var config = _store.Load())
            {
                return config.SFCards.Where(c => c.Type == CardType.Location).ToList();
            }
        }
    }
}
