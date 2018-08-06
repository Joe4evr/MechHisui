using System;
using System.Threading.Tasks;

namespace MechHisui.ExplodingKittens
{
    internal abstract class ExplodingKittensCard
    {
        public string CardName { get; }

        public ExplodingKittensCard(string cardName)
        {
            CardName = cardName;
        }

        public abstract Task Resolve(ExKitGame game);
    }
}
