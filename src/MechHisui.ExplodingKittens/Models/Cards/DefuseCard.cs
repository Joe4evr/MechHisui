using System;
using System.Threading.Tasks;

namespace MechHisui.ExplodingKittens.Cards
{
    internal sealed class DefuseCard : ExplodingKitttensCard
    {
        public DefuseCard()
            : base(ExKitConstants.Defuse)
        {
        }

        public override Task Resolve(ExKitGame game)
        {
            return Task.CompletedTask;
        }
    }
}
