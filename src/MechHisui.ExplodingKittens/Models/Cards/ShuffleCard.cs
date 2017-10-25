using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MechHisui.ExplodingKittens.Cards
{
    internal sealed class ShuffleCard : ExplodingKitttensCard
    {
        public ShuffleCard() : base(ExKitConstants.Shuffle)
        {
        }

        public override Task Resolve(ExKitGame game)
        {

            return game.Channel.SendMessageAsync("The deck has been reshuffled.");
        }
    }
}
