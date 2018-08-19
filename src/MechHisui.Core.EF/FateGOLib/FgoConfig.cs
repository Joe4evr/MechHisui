using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Discord.Addons.SimplePermissions;
using JiiLib.SimpleDsl;
using MechHisui.FateGOLib;
using SharedExtensions;

namespace MechHisui.Core
{
    public sealed class FgoConfig : IFgoConfig
    {
        private readonly IConfigStore<MechHisuiConfig> _store;
        private readonly IServiceProvider _services;

        public FgoConfig(IConfigStore<MechHisuiConfig> store, IServiceProvider services)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        //reading operations
        //Servants
        async Task<IEnumerable<IServantProfile>> IFgoConfig.GetAllServantsAsync()
        {
            using (var config = _store.Load(_services))
            {
                return await config.Servants
                    .AsNoTracking()
                    .WithIncludes()
                    .ToListAsync().ConfigureAwait(false);
            }
        }

        async Task<IEnumerable<string>> IFgoConfig.SearchServantsAsync(QueryParseResult<IServantProfile> options)
        {
            using (var config = _store.Load(_services))
            {
                var servants = await config.Servants
                    .AsNoTracking()
                    .WithIncludes()
                    .ToListAsync().ConfigureAwait(false);

                return options.Apply(servants).ToList();
            }
        }

        async Task<IServantProfile> IFgoConfig.GetServantAsync(int id)
        {
            using (var config = _store.Load(_services))
            {
                return await config.Servants
                    .AsNoTracking()
                    .WithIncludes()
                    .SingleOrDefaultAsync(s => s.Id == id).ConfigureAwait(false);
            }
        }

        async Task<IEnumerable<IServantProfile>> IFgoConfig.FindServantsAsync(string name)
        {
            using (var config = _store.Load(_services))
            {
                return await config.Servants
                    .AsNoTracking()
                    .WithIncludes()
                    .Where(ServantNameMatchingExpression(name))
                    .ToListAsync().ConfigureAwait(false);
            }
        }

        //CEs
        async Task<IEnumerable<ICEProfile>> IFgoConfig.GetAllCEsAsync()
        {
            using (var config = _store.Load(_services))
            {
                return await config.CEs
                    .AsNoTracking()
                    .WithIncludes()
                    .ToListAsync().ConfigureAwait(false);
            }
        }

        async Task<IEnumerable<string>> IFgoConfig.SearchCEsAsync(QueryParseResult<ICEProfile> options)
        {
            using (var config = _store.Load(_services))
            {
                var ces = await config.CEs
                    .AsNoTracking()
                    .WithIncludes()
                    .ToListAsync().ConfigureAwait(false);

                return options.Apply(ces).ToList();
            }
        }

        async Task<ICEProfile> IFgoConfig.GetCEAsync(int id)
        {
            using (var config = _store.Load(_services))
            {
                var range = await config.CERanges
                    .AsNoTracking()
                    .SingleOrDefaultAsync(r => r.LowId <= id && id <= r.HighId).ConfigureAwait(false);
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
            using (var config = _store.Load(_services))
            {
                return await config.CEs
                    .AsNoTracking()
                    .WithIncludes()
                    .Where(CENameMatchingExpression(name))
                    //.Where(c => RegexMatchOneWord(c.Name, name) || c.Aliases.Any(a => RegexMatchOneWord(a.Alias, name)))
                    .ToListAsync().ConfigureAwait(false);
            }
        }

        async Task<IEnumerable<ICEProfile>> IFgoConfig.FindCEsByEffectAsync(string effect)
        {
            using (var config = _store.Load(_services))
            {
                if (effect.Equals("event", StringComparison.OrdinalIgnoreCase))
                {
                    return await config.CEs
                        .AsNoTracking()
                        .WithIncludes()
                        .Where(c => EF.Functions.Like(c.EventEffect, "_%"))
                        .ToListAsync().ConfigureAwait(false);
                }
                else
                {
                    string pattern = $"%{effect}%";
                    return await config.CEs
                        .AsNoTracking()
                        .WithIncludes()
                        .Where(c => EF.Functions.Like(c.Effect, pattern))
                        .ToListAsync().ConfigureAwait(false);
                }
            }
        }

        //Mystic Codes
        async Task<IEnumerable<IMysticCode>> IFgoConfig.GetAllMysticsAsync()
        {
            using (var config = _store.Load(_services))
            {
                return await config.MysticCodes
                    .AsNoTracking()
                    .WithIncludes()
                    .ToListAsync().ConfigureAwait(false);
            }
        }

        async Task<IMysticCode> IFgoConfig.GetMysticAsync(int id)
        {
            using (var config = _store.Load(_services))
            {
                return await config.MysticCodes
                    .AsNoTracking()
                    .WithIncludes()
                    .SingleOrDefaultAsync(m => m.Id == id).ConfigureAwait(false);
            }
        }

        async Task<IEnumerable<IMysticCode>> IFgoConfig.FindMysticsAsync(string name)
        {
            using (var config = _store.Load(_services))
            {
                return await config.MysticCodes
                    .AsNoTracking()
                    .WithIncludes()
                    .Where(MysticNameMatchingExpression(name))
                    //.Where(m => RegexMatchOneWord(m.Name, name) || m.Aliases.Any(a => RegexMatchOneWord(a.Alias, name)))
                    .ToListAsync().ConfigureAwait(false);
            }
        }

        //Events
        async Task<IEnumerable<IFgoEvent>> IFgoConfig.GetAllEventsAsync()
        {
            using (var config = _store.Load(_services))
            {
                return await config.FgoEvents
                    .AsNoTracking()
                    .WithIncludes()
                    .ToListAsync().ConfigureAwait(false);
            }
        }

        async Task<IEnumerable<IFgoEvent>> IFgoConfig.GetCurrentEventsAsync()
        {
            using (var config = _store.Load(_services))
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
            using (var config = _store.Load(_services))
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
        //Servants
        async Task<bool> IFgoConfig.AddServantAliasAsync(string servant, string alias)
        {
            using (var config = _store.Load(_services))
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

        //CEs
        async Task<bool> IFgoConfig.AddCEAliasAsync(string name, string alias)
        {
            using (var config = _store.Load(_services))
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

        //Mystic Codes
        async Task<bool> IFgoConfig.AddMysticAliasAsync(string code, string alias)
        {
            using (var config = _store.Load(_services))
            {
                var mystic = await config.MysticCodes
                    .AsNoTracking()
                    .WithIncludes()
                    .SingleOrDefaultAsync(m => m.Name == code).ConfigureAwait(false);

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

        //Events
        async Task<IFgoEvent> IFgoConfig.AddEventAsync(string name, EventProperties eventProperties)
        {
            using (var config = _store.Load(_services))
            {
                var ev = new FgoEvent
                {
                    EventName = name,
                    StartTime = eventProperties.Start?.ToDateTimeOffset(),
                    EndTime = eventProperties.End?.ToDateTimeOffset(),
                    InfoLink = eventProperties.Info
                };
                await config.FgoEvents.AddAsync(ev).ConfigureAwait(false);
                await config.SaveChangesAsync().ConfigureAwait(false);

                return ev;
            }
        }
        async Task<IFgoEvent> IFgoConfig.EditEventAsync(int id, EventProperties updatedProperties)
        {
            using (var config = _store.Load(_services))
            {
                var ev = await config.FgoEvents
                    .WithIncludes()
                    .SingleOrDefaultAsync(e => e.Id == id).ConfigureAwait(false);

                if (ev == null)
                    throw new InvalidOperationException($"No event with ID '{id}'.");

                if (updatedProperties.Start.HasValue)
                    ev.StartTime = updatedProperties.Start?.ToDateTimeOffset();
                if (updatedProperties.End.HasValue)
                    ev.EndTime = updatedProperties.End?.ToDateTimeOffset();
                if (!String.IsNullOrWhiteSpace(updatedProperties.Info))
                    ev.InfoLink = updatedProperties.Info;

                config.FgoEvents.Update(ev);
                await config.SaveChangesAsync().ConfigureAwait(false);

                return ev;
            }
        }

        //helpers
        private static Expression NameMatchingExpressionBody(MemberExpression member, string name)
        {
            string pattern1 = $"{name} %";
            string pattern2 = $"% {name}";
            string pattern3 = $"% {name} %";

            return Expression.OrElse(
                Expression.Call(_likeFunction, _dbfuncs, member, Expression.Constant(name)),
                Expression.OrElse(
                    Expression.Call(_likeFunction, _dbfuncs, member, Expression.Constant(pattern1)),
                    Expression.OrElse(
                        Expression.Call(_likeFunction, _dbfuncs, member, Expression.Constant(pattern2)),
                        Expression.Call(_likeFunction, _dbfuncs, member, Expression.Constant(pattern3)))));
        }
        private static Expression<Func<ServantProfile, bool>> ServantNameMatchingExpression(string name)
            => Expression.Lambda<Func<ServantProfile, bool>>(NameMatchingExpressionBody(_spNamePropExpr, name), _spParamExpr);
        private static Expression<Func<CEProfile, bool>> CENameMatchingExpression(string name)
            => Expression.Lambda<Func<CEProfile, bool>>(NameMatchingExpressionBody(_ceNamePropExpr, name), _ceParamExpr);
        private static Expression<Func<MysticCode, bool>> MysticNameMatchingExpression(string name)
            => Expression.Lambda<Func<MysticCode, bool>>(NameMatchingExpressionBody(_mysNamePropExpr, name), _spParamExpr);

        private static readonly MethodInfo _likeFunction = typeof(DbFunctionsExtensions).GetMethod("Like", new Type[] { typeof(DbFunctions), typeof(string), typeof(string) });

        private static readonly Type _spType = typeof(ServantProfile);
        private static readonly Type _ceType = typeof(CEProfile);
        private static readonly Type _mysType = typeof(MysticCode);

        private static readonly ConstantExpression _dbfuncs = Expression.Constant(EF.Functions);
        private static readonly ParameterExpression _spParamExpr = Expression.Parameter(_spType, "s");
        private static readonly MemberExpression _spNamePropExpr = Expression.Property(_spParamExpr, _spType.GetProperty("Name"));
        private static readonly ParameterExpression _ceParamExpr = Expression.Parameter(_ceType, "ce");
        private static readonly MemberExpression _ceNamePropExpr = Expression.Property(_ceParamExpr, _ceType.GetProperty("Name"));
        private static readonly ParameterExpression _mysParamExpr = Expression.Parameter(_mysType, "m");
        private static readonly MemberExpression _mysNamePropExpr = Expression.Property(_mysParamExpr, _mysType.GetProperty("Name"));
    }
}
