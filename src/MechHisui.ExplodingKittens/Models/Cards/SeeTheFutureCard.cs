using System;
using System.Threading.Tasks;

namespace MechHisui.ExplodingKittens.Cards
{
    internal sealed class SeeTheFutureCard : ExplodingKittensCard
    {
        public SeeTheFutureCard()
            : base(ExKitConstants.SeeTheFuture)
        {
        }

        public override Task Resolve(ExKitGame game)
        {
            var msg = $"The top three cards are:\n{String.Join("\n", game.PeekTop(3))}";
            return game.TurnPlayer.Value.SendMessageAsync(msg);
        }
    }
}
