using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MechHisui.ExplodingKittens.Cards
{
    internal sealed class AttackCard : ExplodingKitttensCard
    {
        public AttackCard() : base(ExKitConstants.Attack)
        {
        }

        public override async Task Resolve(ExKitGame game)
        {
            var current = game.TurnPlayer;

            ExKitPlayer temp;
            do temp = game.Reverse ? current.Previous.Value : current.Next.Value;
            while (!temp.HasExploded);

            var next = temp;
            next.IsAttacked = true;

            if (current.Value.IsAttacked)
            {
                current.Value.IsAttacked = false;
                await game.Channel.SendMessageAsync($"**{current.Value.User.Username}** has passed on the Attack to **{next.User.Username}**.");
            }
            else
            {
                await game.Channel.SendMessageAsync($"**{current.Value.User.Username}** has attacked **{next.User.Username}**.");
            }
            await game.EndTurnWithoutDraw();
        }
    }
}
