using System;
using Discord.Addons.MpGame.Collections;

namespace MechHisui.ExplodingKittens
{
    internal class ExKitDiscard : Pile<ExplodingKittensCard>
    {
        public override bool CanBrowse     { get; } = true;
        public override bool CanClear      { get; } = false;
        public override bool CanCut        { get; } = false;
        public override bool CanDraw       { get; } = false;
        public override bool CanDrawBottom { get; } = false;
        public override bool CanInsert     { get; } = false;
        public override bool CanPeek       { get; } = false;
        public override bool CanPut        { get; } = true;
        public override bool CanPutBottom  { get; } = false;
        public override bool CanShuffle    { get; } = false;
        public override bool CanTake       { get; } = false;
    }
}
