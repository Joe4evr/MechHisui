using System;
using System.Collections.Generic;
using System.Linq;

namespace MechHisui.FateGOLib
{
    public sealed class ServantFilterOptions
    {
        private static readonly Func<IServantProfile, int> _defaultOrderBy = (s => s.Id);
        private static readonly Func<IServantProfile, string> _defaultSelector = (s => s.Name);
        private static readonly Func<IServantProfile, bool> _defaultPredicate = (s => true);

        public Func<IEnumerable<IServantProfile>, IOrderedEnumerable<IServantProfile>> Order { get; internal set; } = (ss => ss.OrderBy(_defaultOrderBy));
        public Func<IServantProfile, bool> Predicate { get; internal set; } = _defaultPredicate;
        public Func<IServantProfile, string> Selector { get; internal set; } = _defaultSelector;
    }

    public static class ServantFilterExtensions
    {
        public static IEnumerable<string> ApplyFilters(
            this IEnumerable<IServantProfile> profiles,
            ServantFilterOptions options)
            => options.Order(profiles.Where(options.Predicate))
                .Select(options.Selector);
    }
}
