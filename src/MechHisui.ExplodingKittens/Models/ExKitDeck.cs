using System;
using System.Collections.Generic;
using Discord.Addons.MpGame.Collections;

namespace MechHisui.ExplodingKittens
{
    internal class ExKitDeck : Pile<ExplodingKittensCard>
    {
        public override bool CanBrowse     { get; } = false;
        public override bool CanClear      { get; } = false;
        public override bool CanCut        { get; } = false;
        public override bool CanDraw       { get; } = true;
        public override bool CanDrawBottom { get; } = true;
        public override bool CanInsert     { get; } = true;
        public override bool CanPeek       { get; } = true;
        public override bool CanPut        { get; } = false;
        public override bool CanPutBottom  { get; } = true;
        public override bool CanShuffle    { get; } = true;
        public override bool CanTake       { get; } = true;

        public ExKitDeck(IEnumerable<ExplodingKittensCard> cards)
            : base(cards)
        {
        }
    }
}
