using System;
using System.Threading.Tasks;

namespace MechHisui.ExplodingKittens.Cards
{
    internal sealed class ShuffleCard : ExplodingKitttensCard
    {
        public ShuffleCard()
            : base(ExKitConstants.Shuffle)
        {
        }

        public override Task Resolve(ExKitGame game)
        {
            game.Reshuffle();
            return game.Channel.SendMessageAsync("The deck has been reshuffled.");
        }
    }
}
