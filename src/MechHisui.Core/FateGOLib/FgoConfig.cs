using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
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


        public IEnumerable<IServantProfile> AllServants()
        {
            using (var config = _store.Load())
            {
                return QueryServants(config).ToList();
            }
        }

        public IServantProfile GetServant(int id)
        {
            using (var config = _store.Load())
            {
                return QueryServants(config).SingleOrDefault(s => s.Id == id);
            }
        }

        public IEnumerable<IServantProfile> FindServants(string name)
        {
            using (var config = _store.Load())
            {
                return QueryServants(config).Where(s => RegexMatchOneWord(s.Name, name) || s.Aliases.Any(a => RegexMatchOneWord(a.Alias, name))).ToList();
            }
        }

        public bool AddServantAlias(string servant, string alias)
        {
            using (var config = _store.Load())
            {
                var srv = config.Servants
                    .Include(s => s.Aliases)
                    .SingleOrDefault(s => s.Name == servant);
                if (srv == null)
                {
                    return false;
                }
                else
                {
                    var newalias = new ServantAlias { Servant = srv, Alias = alias };
                    config.ServantAliases.Add(newalias);
                    //srv.Aliases.Add(newalias);
                    config.SaveChanges();
                    return true;
                }
            }
        }


        public IEnumerable<ICEProfile> AllCEs()
        {
            using (var config = _store.Load())
            {
                return QueryCEs(config).ToList();
            }
        }

        public ICEProfile GetCE(int id)
        {
            using (var config = _store.Load())
            {
                return QueryCEs(config).SingleOrDefault(c => c.Id == id);
            }
        }

        public IEnumerable<ICEProfile> FindCEs(string name)
        {
            using (var config = _store.Load())
            {
                return QueryCEs(config).Where(c => RegexMatchOneWord(c.Name, name) || c.Aliases.Any(a => RegexMatchOneWord(a.Alias, name))).ToList();
            }
        }

        public bool AddCEAlias(string name, string alias)
        {
            using (var config = _store.Load())
            {
                var ce = QueryCEs(config).SingleOrDefault(c => c.Name == name);

                if (ce == null)
                {
                    return false;
                }
                else
                {
                    var newalias = new CEAlias { CE = ce, Alias = alias };
                    config.CEAliases.Add(newalias);
                    //ce.Aliases.Add(newalias);
                    config.SaveChanges();
                    return true;
                }
            }
        }


        public IEnumerable<IMysticCode> AllMystics()
        {
            using (var config = _store.Load())
            {
                return QueryMystic(config).ToList();
            }
        }

        public IMysticCode GetMystic(int id)
        {
            using (var config = _store.Load())
            {
                return QueryMystic(config).SingleOrDefault(m => m.Id == id);
            }
        }

        public IEnumerable<IMysticCode> FindMystics(string name)
        {
            using (var config = _store.Load())
            {
                return QueryMystic(config).Where(m => RegexMatchOneWord(m.Code, name) || m.Aliases.Any(a => RegexMatchOneWord(a.Alias, name))).ToList();
            }
        }

        public bool AddMysticAlias(string code, string alias)
        {
            using (var config = _store.Load())
            {
                var mystic = QueryMystic(config).SingleOrDefault(m => m.Code == code);

                if (mystic == null)
                {
                    return false;
                }
                else
                {
                    var newalias = new MysticAlias { Code = mystic, Alias = alias };
                    config.MysticAliases.Add(newalias);
                    //mystic.Aliases.Add(newalias);
                    config.SaveChanges();
                    return true;
                }
            }
        }


        public IEnumerable<IFgoEvent> AllEvents()
        {
            using (var config = _store.Load())
            {
                return config.FgoEvents.ToList();
            }
        }

        public IEnumerable<IFgoEvent> GetCurrentEvents()
        {
            using (var config = _store.Load())
            {
                var now = DateTime.UtcNow;
                return config.FgoEvents.Where(e => e.EndTime.HasValue && !(e.EndTime < now)).ToList();
            }
        }


        //query helpers
        private static bool RegexMatchOneWord(string hay, string needle)
                => Regex.Match(hay, String.Concat(_b, needle, _b), RegexOptions.IgnoreCase).Success;

        private const string _b = @"\b";

        private static IQueryable<ServantProfile> QueryServants(MechHisuiConfig config)
        {
            return config.Servants
                .Include(s => s.Traits).ThenInclude(t => t.Trait)
                .Include(s => s.ActiveSkills).ThenInclude(a => a.Skill)
                .Include(s => s.PassiveSkills).ThenInclude(p => p.Skill)
                .Include(s => s.Aliases)
                .Include(s => s.Bond10);
        }

        private static IQueryable<CEProfile> QueryCEs(MechHisuiConfig config)
            => config.CEs.Include(c => c.Aliases);

        private static IQueryable<MysticCode> QueryMystic(MechHisuiConfig config)
            => config.MysticCodes.Include(m => m.Aliases);
    }
}
