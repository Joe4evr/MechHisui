using System;
using System.Collections.Generic;
using Discord.Addons.MpGame.Collections;

namespace MechHisui.SecretHitler.Models
{
    internal sealed class PolicyDeck : Pile<PolicyCard>
    {
        internal PolicyDeck(IEnumerable<PolicyCard> cards)
            : base(cards)
        {
        }

        public override bool CanBrowse     { get; } = false;
        public override bool CanClear      { get; } = true;
        public override bool CanCut        { get; } = false;
        public override bool CanDraw       { get; } = true;
        public override bool CanDrawBottom { get; } = false;
        public override bool CanInsert     { get; } = false;
        public override bool CanPeek       { get; } = true;
        public override bool CanPut        { get; } = false;
        public override bool CanPutBottom  { get; } = false;
        public override bool CanShuffle    { get; } = true;
        public override bool CanTake       { get; } = false;
    }
}
