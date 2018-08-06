using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using MechHisui.Core;

namespace MechHisui.FateGOLib
{
    internal static class QueryHelpers
    {
        public static IQueryable<ServantProfile> WithIncludes(this IQueryable<ServantProfile> profiles)
            => profiles.Include(s => s.Traits).ThenInclude(t => t.Trait)
                .Include(s => s.ActiveSkills).ThenInclude(a => a.Skill)
                .Include(s => s.PassiveSkills).ThenInclude(p => p.Skill)
                .Include(s => s.Aliases)
                .Include(s => s.Bond10);

        public static IQueryable<CEProfile> WithIncludes(this IQueryable<CEProfile> ces)
            => ces.Include(c => c.Aliases);

        public static IQueryable<MysticCode> WithIncludes(this IQueryable<MysticCode> mystics)
            => mystics.Include(m => m.Aliases);

        public static IQueryable<FgoEvent> WithIncludes(this IQueryable<FgoEvent> events)
            => events.Include(e => e.EventGachas)
                .ThenInclude(g => g.RateUpServants)
                .ThenInclude(r => r.Servant);
    }
}
