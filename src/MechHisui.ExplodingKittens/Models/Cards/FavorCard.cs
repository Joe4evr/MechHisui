using System;
using System.Threading.Tasks;

namespace MechHisui.ExplodingKittens.Cards
{
    internal sealed class FavorCard : ExplodingKitttensCard
    {
        public FavorCard()
            : base(ExKitConstants.Favor)
        {
        }

        public override Task Resolve(ExKitGame game)
        {
            game.SetState(GameState.ChooseFavoredPlayer);
            return game.Channel.SendMessageAsync($"**{game.TurnPlayer.Value.User.Username}** may choose a player whom will give him/her a card.");
        }
    }
}
