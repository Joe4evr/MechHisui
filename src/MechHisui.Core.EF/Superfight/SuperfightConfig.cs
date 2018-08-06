//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using Discord.Addons.SimplePermissions;
//using MechHisui.Superfight;

//namespace MechHisui.Core
//{
//    public sealed class SuperfightConfig : ISuperfightConfig
//    {
//        private readonly IConfigStore<MechHisuiConfig> _store;

//        public SuperfightConfig(IConfigStore<MechHisuiConfig> store)
//        {
//            _store = store;
//        }

//        //reading operations
//        IEnumerable<ISuperfightCard> ISuperfightConfig.GetAllCards()
//        {
//            using (var config = _store.Load())
//            {
//                return config.SFCards
//                    .AsNoTracking()
//                    .ToList();
//            }
//        }

//        async Task<IEnumerable<ISuperfightCard>> ISuperfightConfig.GetCharactersAsync()
//        {
//            using (var config = _store.Load())
//            {
//                return await config.SFCards
//                    .AsNoTracking()
//                    .Where(c => c.Type == CardType.Character).ToListAsync();
//            }
//        }

//        async Task<IEnumerable<ISuperfightCard>> ISuperfightConfig.GetAbilitiesAsync()
//        {
//            using (var config = _store.Load())
//            {
//                return await config.SFCards
//                    .AsNoTracking()
//                    .Where(c => c.Type == CardType.Ability).ToListAsync();
//            }
//        }

//        async Task<IEnumerable<ISuperfightCard>> ISuperfightConfig.GetLocationsAsync()
//        {
//            using (var config = _store.Load())
//            {
//                return await config.SFCards
//                    .AsNoTracking()
//                    .Where(c => c.Type == CardType.Location).ToListAsync();
//            }
//        }
//    }
//}
