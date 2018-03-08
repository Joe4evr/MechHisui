using System;
using System.Collections.Generic;
using SharedExtensions.Collections;

namespace MechHisui.Superfight
{
    internal sealed class SuperfightDeck<TCard> : Pile<TCard>
        where TCard : class, ISuperfightCard
    {
        public override bool CanBrowse  { get; } = false;
        public override bool CanClear   { get; } = false;
        public override bool CanDraw    { get; } = true;
        public override bool CanInsert  { get; } = false;
        public override bool CanPeek    { get; } = false;
        public override bool CanPut     { get; } = false;
        public override bool CanShuffle { get; } = false;
        public override bool CanTake    { get; } = false;

        public SuperfightDeck(IEnumerable<TCard> cards)
            : base(cards)
        {
        }
    }
}
