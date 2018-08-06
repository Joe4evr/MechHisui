using System;
using System.Threading.Tasks;

namespace MechHisui.ExplodingKittens.Cards
{
    internal sealed class AttackCard : ExplodingKittensCard
    {
        public AttackCard()
            : base(ExKitConstants.Attack)
        {
        }

        public override async Task Resolve(ExKitGame game)
        {
            var current = game.TurnPlayer;

            var next = game.GetFollowupPlayer();
            next.IsAttacked = true;

            if (current.Value.IsAttacked)
            {
                current.Value.IsAttacked = false;
                await game.Channel.SendMessageAsync($"**{current.Value.User.Username}** has passed on the Attack to **{next.User.Username}**.").ConfigureAwait(false);
            }
            else
            {
                await game.Channel.SendMessageAsync($"**{current.Value.User.Username}** has attacked **{next.User.Username}**.").ConfigureAwait(false);
            }
            await game.EndTurnWithoutDraw().ConfigureAwait(false);
        }
    }
}
