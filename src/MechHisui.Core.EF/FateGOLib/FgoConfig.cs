using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Discord.Addons.SimplePermissions;
using MechHisui.FateGOLib;

namespace MechHisui.Core
{
    public sealed class FgoConfig : IFgoConfig
    {
        private readonly IConfigStore<MechHisuiConfig> _store;

        public FgoConfig(IConfigStore<MechHisuiConfig> store)
        {
            _store = store;
        }


        //reading operations
        //Servants
        async Task<IEnumerable<IServantProfile>> IFgoConfig.GetAllServantsAsync()
        {
            using (var config = _store.Load())
            {
                return await config.Servants
                    .AsNoTracking()
                    .WithIncludes()
                    .ToListAsync().ConfigureAwait(false);
            }
        }

        async Task<IEnumerable<string>> IFgoConfig.SearchServantsAsync(ServantFilterOptions options)
        {
            using (var config = _store.Load())
            {
                var servants = await config.Servants
                    .AsNoTracking()
                    .WithIncludes()
                    .ToListAsync()
                    .ConfigureAwait(false);

                return servants.ApplyFilters(options).ToList();
            }
        }

        async Task<IServantProfile> IFgoConfig.GetServantAsync(int id)
        {
            using (var config = _store.Load())
            {
                return await config.Servants
                    .AsNoTracking()
                    .WithIncludes()
                    .SingleOrDefaultAsync(s => s.Id == id).ConfigureAwait(false);
            }
        }

        async Task<IEnumerable<IServantProfile>> IFgoConfig.FindServantsAsync(string name)
        {
            using (var config = _store.Load())
            {
                return await config.Servants
                    .AsNoTracking()
                    .WithIncludes()
                    .Where(s => RegexMatchOneWord(s.Name, name) || s.Aliases.Any(a => RegexMatchOneWord(a.Alias, name)))
                    .ToListAsync().ConfigureAwait(false);
            }
        }

        //CEs
        async Task<IEnumerable<ICEProfile>> IFgoConfig.GetAllCEsAsync()
        {
            using (var config = _store.Load())
            {
                return await config.CEs
                    .AsNoTracking()
                    .WithIncludes()
                    .ToListAsync().ConfigureAwait(false);
            }
        }

        async Task<ICEProfile> IFgoConfig.GetCEAsync(int id)
        {
            using (var config = _store.Load())
            {
                var range = await config.CERanges.SingleOrDefaultAsync(r => r.LowId <= id && id <= r.HighId).ConfigureAwait(false);
                if (range != null)
                {
                    return new CEProfile
                    {
                        Id = id,
                        Rarity = range.Rarity,
                        Name = range.Name,
                        Cost = range.Cost,
                        Atk = range.Atk,
                        HP = range.HP,
                        Effect = range.Effect,
                        EventEffect = range.EventEffect,
                        AtkMax = range.AtkMax,
                        HPMax = range.HPMax,
                        EffectMax = range.EffectMax,
                        EventEffectMax = range.EventEffectMax,
                        Image = range.Image,
                        Obtainable = range.Obtainable,
                        Aliases = Enumerable.Empty<CEAlias>()
                    };
                }

                return await config.CEs
                    .AsNoTracking()
                    .WithIncludes()
                    .SingleOrDefaultAsync(c => c.Id == id).ConfigureAwait(false);
            }
        }

        async Task<IEnumerable<ICEProfile>> IFgoConfig.FindCEsAsync(string name)
        {
            using (var config = _store.Load())
            {
                return await config.CEs
                    .AsNoTracking()
                    .WithIncludes()
                    .Where(c => RegexMatchOneWord(c.Name, name) || c.Aliases.Any(a => RegexMatchOneWord(a.Alias, name)))
                    .ToListAsync().ConfigureAwait(false);
            }
        }

        //Mystic Codes
        async Task<IEnumerable<IMysticCode>> IFgoConfig.GetAllMysticsAsync()
        {
            using (var config = _store.Load())
            {
                return await config.MysticCodes
                    .AsNoTracking()
                    .WithIncludes()
                    .ToListAsync().ConfigureAwait(false);
            }
        }

        async Task<IMysticCode> IFgoConfig.GetMysticAsync(int id)
        {
            using (var config = _store.Load())
            {
                return await config.MysticCodes
                    .AsNoTracking()
                    .WithIncludes()
                    .SingleOrDefaultAsync(m => m.Id == id).ConfigureAwait(false);
            }
        }

        async Task<IEnumerable<IMysticCode>> IFgoConfig.FindMysticsAsync(string name)
        {
            using (var config = _store.Load())
            {
                return await config.MysticCodes
                    .AsNoTracking()
                    .WithIncludes()
                    .Where(m => RegexMatchOneWord(m.Code, name) || m.Aliases.Any(a => RegexMatchOneWord(a.Alias, name)))
                    .ToListAsync().ConfigureAwait(false);
            }
        }

        //Events
        async Task<IEnumerable<IFgoEvent>> IFgoConfig.GetAllEventsAsync()
        {
            using (var config = _store.Load())
            {
                return await config.FgoEvents
                    .AsNoTracking()
                    .WithIncludes()
                    .ToListAsync().ConfigureAwait(false);
            }
        }

        async Task<IEnumerable<IFgoEvent>> IFgoConfig.GetCurrentEventsAsync()
        {
            using (var config = _store.Load())
            {
                var now = DateTime.UtcNow;
                return await config.FgoEvents
                    .AsNoTracking()
                    .Where(e => e.StartTime < now && ((!e.EndTime.HasValue) || e.EndTime > now))
                    .WithIncludes()
                    .ToListAsync().ConfigureAwait(false);
            }
        }

        async Task<IEnumerable<IFgoEvent>> IFgoConfig.GetFutureEventsAsync()
        {
            using (var config = _store.Load())
            {
                var now = DateTime.UtcNow;
                return await config.FgoEvents
                    .AsNoTracking()
                    .Where(e => (!e.StartTime.HasValue) || e.StartTime > now)
                    .WithIncludes()
                    .ToListAsync().ConfigureAwait(false);
            }
        }

        //writing operations
        async Task<bool> IFgoConfig.AddServantAliasAsync(string servant, string alias)
        {
            using (var config = _store.Load())
            {
                var srv = await config.Servants
                    .Include(s => s.Aliases)
                    .SingleOrDefaultAsync(s => s.Name == servant).ConfigureAwait(false);

                if (srv == null)
                {
                    return false;
                }
                else
                {
                    var newalias = new ServantAlias { Servant = srv, Alias = alias };
                    await config.ServantAliases.AddAsync(newalias).ConfigureAwait(false);
                    //srv.Aliases.Add(newalias);
                    await config.SaveChangesAsync().ConfigureAwait(false);
                    return true;
                }
            }
        }

        async Task<bool> IFgoConfig.AddCEAliasAsync(string name, string alias)
        {
            using (var config = _store.Load())
            {
                var ce = await config.CEs
                    .AsNoTracking()
                    .WithIncludes()
                    .SingleOrDefaultAsync(c => c.Name == name).ConfigureAwait(false);

                if (ce == null)
                {
                    return false;
                }
                else
                {
                    var newalias = new CEAlias { CE = ce, Alias = alias };
                    await config.CEAliases.AddAsync(newalias).ConfigureAwait(false);
                    //ce.Aliases.Add(newalias);
                    await config.SaveChangesAsync().ConfigureAwait(false);
                    return true;
                }
            }
        }

        async Task<bool> IFgoConfig.AddMysticAliasAsync(string code, string alias)
        {
            using (var config = _store.Load())
            {
                var mystic = await config.MysticCodes
                    .AsNoTracking()
                    .WithIncludes()
                    .SingleOrDefaultAsync(m => m.Code == code).ConfigureAwait(false);

                if (mystic == null)
                {
                    return false;
                }
                else
                {
                    var newalias = new MysticAlias { Code = mystic, Alias = alias };
                    await config.MysticAliases.AddAsync(newalias).ConfigureAwait(false);
                    //mystic.Aliases.Add(newalias);
                    await config.SaveChangesAsync().ConfigureAwait(false);
                    return true;
                }
            }
        }

        async Task<IFgoEvent> IFgoConfig.AddEventAsync(string name, DateTimeOffset? start, DateTimeOffset? end, string info)
        {
            using (var config = _store.Load())
            {
                var ev = new FgoEvent
                {
                    EventName = name,
                    StartTime = start,
                    EndTime = end,
                    InfoLink = info
                };
                await config.FgoEvents.AddAsync(ev).ConfigureAwait(false);
                await config.SaveChangesAsync().ConfigureAwait(false);

                return ev;
            }
        }

        //helpers
        private static bool RegexMatchOneWord(string hay, string needle)
                => Regex.Match(hay, String.Concat(_b, needle, _b), RegexOptions.IgnoreCase).Success;

        private const string _b = @"\b";
    }
}
