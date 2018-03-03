using System;
using System.Threading.Tasks;

namespace MechHisui.ExplodingKittens.Cards
{
    internal sealed class NopeCard : ExplodingKitttensCard
    {
        public NopeCard() : base(ExKitConstants.Nope)
        {
        }

        public override Task Resolve(ExKitGame game)
        {
            return Task.CompletedTask;
        }
    }
}
