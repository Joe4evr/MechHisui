//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;

//namespace MechHisui.FateGOLib
//{
//    public sealed class ServantFilterOptions
//    {
//        private static readonly Func<IServantProfile, int> _defaultOrderBy = (s => s.Id);
//        private static readonly Func<IServantProfile, string> _defaultSelect = (s => s.Name);

//        public Func<IEnumerable<IServantProfile>, IOrderedEnumerable<IServantProfile>> WithOrderBy { get; internal set; }
//            = (ss => ss.OrderBy(_defaultOrderBy));

//        public Func<IEnumerable<IServantProfile>, IEnumerable<string>> WithSelect { get; internal set; }
//            = (ss => ss.Select(_defaultSelect));

//        public Func<IServantProfile, bool> WithFilters
//            => (s => Predicates.Values.All(p => p(s)));

//        internal ConcurrentDictionary<string, Predicate<IServantProfile>> Predicates { get; }
//            = new ConcurrentDictionary<string, Predicate<IServantProfile>>(StringComparer.OrdinalIgnoreCase);



//        //public bool RunFilters(IServantProfile profile)
//        //{
//        //    return Predicates.Values.All(p => p(profile));
//        //}
//    }
//}
