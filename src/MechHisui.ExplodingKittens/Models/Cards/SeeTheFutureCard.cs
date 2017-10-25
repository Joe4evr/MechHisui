using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MechHisui.ExplodingKittens.Cards
{
    internal sealed class SeeTheFutureCard : ExplodingKitttensCard
    {
        public SeeTheFutureCard() : base(ExKitConstants.SeeTheFuture)
        {
        }

        public override Task Resolve(ExKitGame game)
        {
            var msg = $"The top three cards are:\n{String.Join("\n", game.PeekTop(3))}";
            return game.TurnPlayer.Value.SendMessageAsync(msg);
        }
    }
}
