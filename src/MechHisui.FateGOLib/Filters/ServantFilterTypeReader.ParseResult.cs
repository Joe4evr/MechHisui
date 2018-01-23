//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace MechHisui.FateGOLib
//{
//    public sealed partial class ServantFilterTypeReader
//    {
//        private struct FilterParseResult
//        {
//            public FilterParseResult(bool isSuccess, string error = null)
//            {
//                IsSuccess = isSuccess;
//                ErrorReason = error;
//            }

//            public bool IsSuccess { get; }
//            public string ErrorReason { get; }

//            public static FilterParseResult Success { get; } = new FilterParseResult(true);

//            public static FilterParseResult DuplicateKey(string key)
//                => new FilterParseResult(false, $"Predicate with key '{key}' was aleady added.");
//            public static FilterParseResult NaN(string prop)
//                => new FilterParseResult(false, $"Could not parse value for '{prop}' as a number.");
//            public static FilterParseResult InvalidOp(string prop, Operator op)
//                => new FilterParseResult(false, $"Cannot use {op} operator on '{prop}'.");

//            public static FilterParseResult UnexpectedOp { get; } = new FilterParseResult(false, "Unexpected operator.");
//            public static FilterParseResult UnexpectedExpr { get; } = new FilterParseResult(false, "Unexpected expression.");
//        }

//        private static FilterParseResult TryAdd(ServantFilterOptions options, string key, Predicate<IServantProfile> predicate)
//            => (options.Predicates.TryAdd(key, predicate))
//                    ? FilterParseResult.Success
//                    : FilterParseResult.DuplicateKey(key);

//        private static FilterParseResult ParseIdPredicate(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options)
//        {
//            if (!Int32.TryParse(value.Materialize(), out var num))
//                return FilterParseResult.NaN(prop);

//            switch (op)
//            {
//                case Operator.LessThan:
//                    return TryAdd(options, "IdLessThan", (s => s.Id < num));

//                case Operator.GreaterThan:
//                    return TryAdd(options, "IdGreaterThan", (s => s.Id > num));

//                case Operator.IsEqual:
//                    return new FilterParseResult(false, "Non-sensical to use IsEqual operator on 'Id'.");

//                case Operator.NotEqual:
//                    return TryAdd(options, "IdIsNot", (s => s.Id != num));

//                default:
//                    return FilterParseResult.UnexpectedOp;
//            }
//        }
//        private static FilterParseResult ParseClassPredicate(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options)
//        {
//            switch (op)
//            {
//                case Operator.LessThan:
//                case Operator.GreaterThan:
//                    return FilterParseResult.InvalidOp(prop, op);

//                case Operator.IsEqual:
//                    {
//                        var v = value.Materialize();
//                        return TryAdd(options, "Class", (s => s.Class.Equals(v, StringComparison.OrdinalIgnoreCase)));
//                    }
//                case Operator.NotEqual:
//                    {
//                        var v = value.Materialize();
//                        return TryAdd(options, "Class", (s => !s.Class.Equals(v, StringComparison.OrdinalIgnoreCase)));
//                    }
//                default:
//                    return FilterParseResult.UnexpectedOp;
//            }
//        }
//        private static FilterParseResult ParseRarityPredicate(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options)
//        {
//            if (!Int32.TryParse(value.Materialize(), out var num))
//                return FilterParseResult.NaN(prop);

//            switch (op)
//            {
//                case Operator.LessThan:
//                    return TryAdd(options, "RarityLessThan", (s => s.Rarity < num));

//                case Operator.GreaterThan:
//                    return TryAdd(options, "RarityGreaterThan", (s => s.Rarity > num));

//                case Operator.IsEqual:
//                    return TryAdd(options, "RarityEqual", (s => s.Rarity == num));

//                case Operator.NotEqual:
//                    return TryAdd(options, "RarityNotEqual", (s => s.Rarity != num));

//                default:
//                    return FilterParseResult.UnexpectedOp;
//            }
//        }
//        private static FilterParseResult ParseNamePredicate(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options)
//        {
//            switch (op)
//            {
//                case Operator.LessThan:
//                case Operator.GreaterThan:
//                    return FilterParseResult.InvalidOp(prop, op);

//                case Operator.IsEqual:
//                    {
//                        var v = value.Materialize();
//                        return TryAdd(options, "NameContains", (s => s.Name.Contains(v)));
//                    }
//                case Operator.NotEqual:
//                    {
//                        var v = value.Materialize();
//                        return TryAdd(options, "NameContains", (s => !s.Name.Contains(v)));
//                    }
//                default:
//                    return FilterParseResult.UnexpectedOp;
//            }
//        }
//        private static FilterParseResult ParseAtkPredicate(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options)
//        {
//            if (!Int32.TryParse(value.Materialize(), out var num))
//                return FilterParseResult.NaN(prop);

//            switch (op)
//            {
//                case Operator.LessThan:
//                    return TryAdd(options, "AtkLessThan", (s => s.Atk < num));

//                case Operator.GreaterThan:
//                    return TryAdd(options, "AtkGreaterThan", (s => s.Atk > num));

//                case Operator.IsEqual:
//                    return TryAdd(options, "Atk", (s => s.Atk == num));

//                case Operator.NotEqual:
//                    return TryAdd(options, "Atk", (s => s.Atk != num));

//                default:
//                    return FilterParseResult.UnexpectedOp;
//            }
//        }
//        private static FilterParseResult ParseHPPredicate(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options)
//        {
//            if (!Int32.TryParse(value.Materialize(), out var num))
//                return FilterParseResult.NaN(prop);

//            switch (op)
//            {
//                case Operator.LessThan:
//                    return TryAdd(options, "HPLessThan", (s => s.HP < num));

//                case Operator.GreaterThan:
//                    return TryAdd(options, "HPGreaterThan", (s => s.HP > num));

//                case Operator.IsEqual:
//                    return TryAdd(options, "HP", (s => s.HP == num));

//                case Operator.NotEqual:
//                    return TryAdd(options, "HP", (s => s.HP != num));

//                default:
//                    return FilterParseResult.UnexpectedOp;
//            }
//        }
//        private static FilterParseResult ParseStarweightPredicate(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options)
//        {
//            if (!Int32.TryParse(value.Materialize(), out var num))
//                return FilterParseResult.NaN(prop);

//            switch (op)
//            {
//                case Operator.LessThan:
//                    return TryAdd(options, "StarweightLessThan", (s => s.Starweight < num));

//                case Operator.GreaterThan:
//                    return TryAdd(options, "StarweightGreaterThan", (s => s.Starweight > num));

//                case Operator.IsEqual:
//                    return TryAdd(options, "Starweight", (s => s.Starweight == num));

//                case Operator.NotEqual:
//                    return TryAdd(options, "Starweight", (s => s.Starweight != num));

//                default:
//                    return FilterParseResult.UnexpectedOp;
//            }
//        }
//        private static FilterParseResult ParseGenderPredicate(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options)
//        {
//            switch (op)
//            {
//                case Operator.LessThan:
//                case Operator.GreaterThan:
//                    return FilterParseResult.InvalidOp(prop, op);

//                case Operator.IsEqual:
//                    {
//                        var v = value.Materialize();
//                        return TryAdd(options, "Gender", (s => s.Gender.Equals(v, StringComparison.OrdinalIgnoreCase)));
//                    }
//                case Operator.NotEqual:
//                    {
//                        var v = value.Materialize();
//                        return TryAdd(options, "Gender", (s => !s.Gender.Equals(v, StringComparison.OrdinalIgnoreCase)));
//                    }
//                default:
//                    return FilterParseResult.UnexpectedOp;
//            }
//        }
//        private static FilterParseResult ParseAttributePredicate(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options)
//        {
//            switch (op)
//            {
//                case Operator.LessThan:
//                case Operator.GreaterThan:
//                    return FilterParseResult.InvalidOp(prop, op);

//                case Operator.IsEqual:
//                    {
//                        var v = value.Materialize();
//                        return TryAdd(options, "Attribute", (s => s.Attribute.Equals(v, StringComparison.OrdinalIgnoreCase)));
//                    }
//                case Operator.NotEqual:
//                    {
//                        var v = value.Materialize();
//                        return TryAdd(options, "Attribute", (s => !s.Attribute.Equals(v, StringComparison.OrdinalIgnoreCase)));
//                    }
//                default:
//                    return FilterParseResult.UnexpectedOp;
//            }
//        }
//        private static FilterParseResult ParseGrowthCurvePredicate(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options)
//        {
//            switch (op)
//            {
//                case Operator.LessThan:
//                case Operator.GreaterThan:
//                    return FilterParseResult.InvalidOp(prop, op);

//                case Operator.IsEqual:
//                    {
//                        var v = value.Materialize();
//                        return TryAdd(options, "GrowthCurve", (s => s.GrowthCurve.Equals(v, StringComparison.OrdinalIgnoreCase)));
//                    }
//                case Operator.NotEqual:
//                    {
//                        var v = value.Materialize();
//                        return TryAdd(options, "GrowthCurve", (s => !s.GrowthCurve.Equals(v, StringComparison.OrdinalIgnoreCase)));
//                    }
//                default:
//                    return FilterParseResult.UnexpectedOp;
//            }
//        }


//        //private static FilterParseResult ParseCardPoolPredicate(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options)
//        //{
//        //    switch (op)
//        //    {
//        //        case Operator.LessThan:
//        //        case Operator.GreaterThan:
//        //            return FilterParseResult.InvalidOp(prop, op);

//        //        case Operator.Equal:
//        //            {
//        //                return TryAdd(options, "", (s => ));
//        //            }
//        //        case Operator.NotEqual:
//        //            {
//        //                return TryAdd(options, "", (s => ));
//        //            }
//        //        default:
//        //            return FilterParseResult.UnexpectedOp;
//        //    }
//        //}
//        //private static FilterParseResult ParseBusterPredicate(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options)
//        //{
//        //    if (!Int32.TryParse(value, out var num))
//        //        return new FilterParseResult(false, "Could not parse value for 'B' as a number.");

//        //    switch (op)
//        //    {
//        //        case Operator.GreaterThan:
//        //            options.Predicates["GreaterThan"] = (s => s.Rarity > num);
//        //            return new FilterParseResult(true);
//        //        case Operator.Equal:
//        //            options.Predicates["Equal"] = (s => s.Rarity == num);
//        //            return new FilterParseResult(true);
//        //        case Operator.NotEqual:
//        //            options.Predicates["NotEqual"] = (s => s.Rarity != num);
//        //            return new FilterParseResult(true);
//        //        case Operator.LessThan:
//        //            options.Predicates["LessThan"] = (s => s.Rarity < num);
//        //            return new FilterParseResult(true);
//        //        default:
//        //            return new FilterParseResult(false, "Unexpected operator.");
//        //    }
//        //}
//        //private static FilterParseResult ParseArtsPredicate(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options)
//        //{
//        //    if (!Int32.TryParse(value, out var num))
//        //        return new FilterParseResult(false, "Could not parse value for 'A' as a number.");

//        //    switch (op)
//        //    {
//        //        case Operator.GreaterThan:
//        //            options.Predicates["GreaterThan"] = (s => s.Rarity > num);
//        //            return new FilterParseResult(true);
//        //        case Operator.Equal:
//        //            options.Predicates["Equal"] = (s => s.Rarity == num);
//        //            return new FilterParseResult(true);
//        //        case Operator.NotEqual:
//        //            options.Predicates["NotEqual"] = (s => s.Rarity != num);
//        //            return new FilterParseResult(true);
//        //        case Operator.LessThan:
//        //            options.Predicates["LessThan"] = (s => s.Rarity < num);
//        //            return new FilterParseResult(true);
//        //        default:
//        //            return new FilterParseResult(false, "Unexpected operator.");
//        //    }
//        //}
//        //private static FilterParseResult ParseQuickPredicate(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options)
//        //{
//        //    if (!Int32.TryParse(value, out var num))
//        //        return new FilterParseResult(false, "Could not parse value for 'Q' as a number.");

//        //    switch (op)
//        //    {
//        //        case Operator.GreaterThan:
//        //            options.Predicates["GreaterThan"] = (s => s.Rarity > num);
//        //            return new FilterParseResult(true);
//        //        case Operator.Equal:
//        //            options.Predicates["Equal"] = (s => s.Rarity == num);
//        //            return new FilterParseResult(true);
//        //        case Operator.NotEqual:
//        //            options.Predicates["NotEqual"] = (s => s.Rarity != num);
//        //            return new FilterParseResult(true);
//        //        case Operator.LessThan:
//        //            options.Predicates["LessThan"] = (s => s.Rarity < num);
//        //            return new FilterParseResult(true);
//        //        default:
//        //            return new FilterParseResult(false, "Unexpected operator.");
//        //    }
//        //}
//        //private static FilterParseResult ParseEXPredicate(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options)
//        //{
//        //    if (!Int32.TryParse(value, out var num))
//        //        return new FilterParseResult(false, "Could not parse value for 'EX' as a number.");

//        //    switch (op)
//        //    {
//        //        case Operator.GreaterThan:
//        //            options.Predicates["GreaterThan"] = (s => s.Rarity > num);
//        //            return new FilterParseResult(true);

//        //        case Operator.Equal:
//        //            options.Predicates["Equal"] = (s => s.Rarity == num);
//        //            return new FilterParseResult(true);

//        //        case Operator.NotEqual:
//        //            options.Predicates["NotEqual"] = (s => s.Rarity != num);
//        //            return new FilterParseResult(true);

//        //        case Operator.LessThan:
//        //            options.Predicates["LessThan"] = (s => s.Rarity < num);
//        //            return new FilterParseResult(true);

//        //        default:
//        //            return new FilterParseResult(false, "Unexpected operator.");
//        //    }
//        //}


//        //private static FilterParseResult ParseNPTypePredicate(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options)
//        //{
//        //    switch (op)
//        //    {
//        //        case Operator.LessThan:
//        //        case Operator.GreaterThan:
//        //            return FilterParseResult.InvalidOp(prop, op);

//        //        case Operator.Equal:
//        //            return TryAdd(options, "", (s => ));

//        //        case Operator.NotEqual:
//        //            return TryAdd(options, "", (s => ));

//        //        default:
//        //            return FilterParseResult.UnexpectedOp;
//        //    }
//        //}
//        //private static FilterParseResult ParseNoblePhantasmPredicate(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options)
//        //{
//        //    switch (op)
//        //    {
//        //        case Operator.LessThan:
//        //        case Operator.GreaterThan:
//        //            return FilterParseResult.InvalidOp(prop, op);

//        //        case Operator.Equal:
//        //            return TryAdd(options, "", (s => ));

//        //        case Operator.NotEqual:
//        //            return TryAdd(options, "", (s => ));

//        //        default:
//        //            return FilterParseResult.UnexpectedOp;
//        //    }
//        //}
//        //private static FilterParseResult ParseNPEffectPredicate(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options)
//        //{
//        //    switch (op)
//        //    {
//        //        case Operator.LessThan:
//        //        case Operator.GreaterThan:
//        //            return FilterParseResult.InvalidOp(prop, op);

//        //        case Operator.Equal:
//        //            return TryAdd(options, "", (s => ));

//        //        case Operator.NotEqual:
//        //            return TryAdd(options, "", (s => ));

//        //        default:
//        //            return FilterParseResult.UnexpectedOp;
//        //    }
//        //}


//        //private static FilterParseResult ParseObtainablePredicate(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options)
//        //{

//        //}
//        //private static FilterParseResult ParseTraitsPredicate(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options)
//        //{
//        //    switch (op)
//        //    {
//        //        case Operator.LessThan:
//        //        case Operator.GreaterThan:
//        //            return FilterParseResult.InvalidOp(prop, op);

//        //        case Operator.Equal:
//        //            return TryAdd(options, "", (s => ));

//        //        case Operator.NotEqual:
//        //            return TryAdd(options, "", (s => ));

//        //        default:
//        //            return FilterParseResult.UnexpectedOp;
//        //    }
//        //}
//        //private static FilterParseResult ParseActiveSkillsPredicate(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options)
//        //{
//        //    switch (op)
//        //    {
//        //        case Operator.LessThan:
//        //        case Operator.GreaterThan:
//        //            return FilterParseResult.InvalidOp(prop, op);

//        //        case Operator.Equal:
//        //            return TryAdd(options, "", (s => ));

//        //        case Operator.NotEqual:
//        //            return TryAdd(options, "", (s => ));

//        //        default:
//        //            return FilterParseResult.UnexpectedOp;
//        //    }
//        //}
//        //private static FilterParseResult ParsePassiveSkillsPredicate(string prop, Operator op, ReadOnlySpan<char> value, ServantFilterOptions options)
//        //{
//        //    switch (op)
//        //    {
//        //        case Operator.LessThan:
//        //        case Operator.GreaterThan:
//        //            return FilterParseResult.InvalidOp(prop, op);

//        //        case Operator.Equal:
//        //            return TryAdd(options, "", (s => ));

//        //        case Operator.NotEqual:
//        //            return TryAdd(options, "", (s => ));

//        //        default:
//        //            return FilterParseResult.UnexpectedOp;
//        //    }
//        //}

//        private static FilterParseResult ParseOrderByClause(ReadOnlySpan<char> value, ServantFilterOptions options)
//        {
//            var prop = value.SliceUntil(' ', out var desc).Materialize();
//            var descending = desc.SequenceEqual("desc".AsReadOnlySpan()); //order descending

//            switch (prop)
//            {
//                case nameof(IServantProfile.Id):
//                    options.WithOrderBy = Create(s => s.Id);
//                    return FilterParseResult.Success;

//                case nameof(IServantProfile.Rarity):
//                    options.WithOrderBy = Create(s => s.Rarity);
//                    return FilterParseResult.Success;

//                case nameof(IServantProfile.Atk):
//                    options.WithOrderBy = Create(s => s.Atk);
//                    return FilterParseResult.Success;

//                case nameof(IServantProfile.HP):
//                    options.WithOrderBy = Create(s => s.HP);
//                    return FilterParseResult.Success;

//                case nameof(IServantProfile.Starweight):
//                    options.WithOrderBy = Create(s => s.Starweight);
//                    return FilterParseResult.Success;

//                case nameof(IServantProfile.B):
//                    options.WithOrderBy = Create(s => s.B);
//                    return FilterParseResult.Success;

//                case nameof(IServantProfile.A):
//                    options.WithOrderBy = Create(s => s.A);
//                    return FilterParseResult.Success;

//                case nameof(IServantProfile.Q):
//                    options.WithOrderBy = Create(s => s.Q);
//                    return FilterParseResult.Success;

//                case nameof(IServantProfile.EX):
//                    options.WithOrderBy = Create(s => s.EX);
//                    return FilterParseResult.Success;

//                default:
//                    return new FilterParseResult(false, "Could not order by that property.");
//            }

//            Func<IEnumerable<IServantProfile>, IOrderedEnumerable<IServantProfile>> Create(Func<IServantProfile, int> clause)
//            {
//                if (descending)
//                {
//                    return (ss => ss.OrderByDescending(clause));
//                }
//                else
//                {
//                    return (ss => ss.OrderBy(clause));
//                }
//            }
//        }

//        //private static FilterParseResult ParseSelectClause(ReadOnlySpan<char> value, ServantFilterOptions options)
//        //{


//        //    Func<IEnumerable<IServantProfile>, IEnumerable<string>> Create(Func<IServantProfile, string> clause)
//        //        => (ss => ss.Select(clause));
//        //}
//    }
//}
