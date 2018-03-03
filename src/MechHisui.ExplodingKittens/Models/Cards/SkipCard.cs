﻿using System;
using System.Threading.Tasks;

namespace MechHisui.ExplodingKittens.Cards
{
    internal sealed class SkipCard : ExplodingKitttensCard
    {
        public SkipCard()
            : base(ExKitConstants.Skip)
        {
        }

        public override Task Resolve(ExKitGame game)
            => game.EndTurnWithoutDraw();
    }
}
