using System;
using System.Threading.Tasks;

namespace MechHisui.ExplodingKittens.Cards
{
    internal sealed class ExplodingKitten : ExplodingKittensCard
    {
        public ExplodingKitten()
            : base(ExKitConstants.ExplodingKitten)
        {
        }

        public override Task Resolve(ExKitGame game)
        {
            return Task.CompletedTask;
        }
    }
}
