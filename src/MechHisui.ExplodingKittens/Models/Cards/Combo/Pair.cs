using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MechHisui.ExplodingKittens.Cards
{
    internal sealed class Pair : ExplodingKitttensCard
    {
        public ExplodingKitttensCard One { get; }
        public ExplodingKitttensCard Two { get; }

        public Pair(ExplodingKitttensCard one, ExplodingKitttensCard two) : base(ExKitConstants.Pair)
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
