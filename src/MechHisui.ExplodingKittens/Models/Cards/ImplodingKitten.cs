using System.Threading.Tasks;

namespace MechHisui.ExplodingKittens.Cards
{
    internal class ImplodingKitten : ExplodingKitttensCard
    {
        public bool IsFaceUp { get; private set; } = false;

        public ImplodingKitten()
            : base(ExKitConstants.ImplodingKitten)
        {
        }

        public void SetFaceUp() => IsFaceUp = true;

        public override Task Resolve(ExKitGame game)
        {
            return Task.CompletedTask;
        }
    }
}
