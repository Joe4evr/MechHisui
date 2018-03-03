using System;
using System.Threading.Tasks;

namespace MechHisui.ExplodingKittens.Cards
{
    internal abstract class ExplodingKitttensCard
    {
        public string CardName { get; }

        public ExplodingKitttensCard(string cardName)
        {
            CardName = cardName;
        }

        public abstract Task Resolve(ExKitGame game);
    }
}
