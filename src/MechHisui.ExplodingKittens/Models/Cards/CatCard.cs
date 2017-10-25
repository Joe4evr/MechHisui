﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MechHisui.ExplodingKittens.Cards
{
    internal sealed class CatCard : ExplodingKitttensCard
    {
        public CatCard(string cardName) : base(cardName)
        {
        }

        public override Task Resolve(ExKitGame game)
        {
            return Task.CompletedTask;
        }
    }
}
