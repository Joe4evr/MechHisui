using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MechHisui.ExplodingKittens.Cards
{
    internal sealed class ThreeOfAKind : ExplodingKitttensCard
    {
        public ExplodingKitttensCard One { get; }
        public ExplodingKitttensCard Two { get; }
        public ExplodingKitttensCard Three { get; }

        public ThreeOfAKind(ExplodingKitttensCard one, ExplodingKitttensCard two, ExplodingKitttensCard three) : base(ExKitConstants.ThreeOfAKind)
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
