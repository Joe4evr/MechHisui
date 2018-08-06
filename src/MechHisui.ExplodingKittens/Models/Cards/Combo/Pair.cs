using System;
using System.Threading.Tasks;

namespace MechHisui.ExplodingKittens.Cards
{
    internal sealed class Pair : ExplodingKittensCard
    {
        public ExplodingKittensCard One { get; }
        public ExplodingKittensCard Two { get; }

        public Pair(ExplodingKittensCard one, ExplodingKittensCard two)
            : base(ExKitConstants.Pair)
        {
            One = one;
            Two = two;
        }

        public override Task Resolve(ExKitGame game)
        {
            game.SetState(GameState.StealRandomCard);
            return game.Channel.SendMessageAsync($"**{game.TurnPlayer.Value.User.Username}** may steal a random card from a player of their choosing.");
        }
    }
}
