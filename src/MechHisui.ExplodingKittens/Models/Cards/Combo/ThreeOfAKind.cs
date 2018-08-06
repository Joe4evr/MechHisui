using System;
using System.Threading.Tasks;

namespace MechHisui.ExplodingKittens.Cards
{
    internal sealed class ThreeOfAKind : ExplodingKittensCard
    {
        public ExplodingKittensCard One { get; }
        public ExplodingKittensCard Two { get; }
        public ExplodingKittensCard Three { get; }

        public ThreeOfAKind(ExplodingKittensCard one, ExplodingKittensCard two, ExplodingKittensCard three)
            : base(ExKitConstants.ThreeOfAKind)
        {
            One = one;
            Two = two;
            Three = three;
        }

        public override Task Resolve(ExKitGame game)
        {
            game.SetState(GameState.StealChosenCard);
            return game.Channel.SendMessageAsync($"**{game.TurnPlayer.Value.User.Username}** may steal a card of their choosing from a player of their choosing.");
        }
    }
}
