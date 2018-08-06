using System.Collections.Generic;
using MechHisui.ExplodingKittens.Cards;

namespace MechHisui.ExplodingKittens
{
    internal static class ExKitConstants
    {
        //Effect Cards OG
        public const string ExplodingKitten = "Exploding Kitten";
        public const string Defuse          = "Defuse";
        public const string Nope            = "Nope";
        public const string Attack          = "Attack";
        public const string Skip            = "Skip";
        public const string Favor           = "Favor";
        public const string Shuffle         = "Shuffle";
        public const string SeeTheFuture    = "See The Future";

        //Effect Cards Additional
        public const string FeralCat        = "Feral Cat";
        public const string TargetedAttacks = "Targeted Attacks";
        public const string AlterTheFuture  = "Alter The Future";
        public const string DrawFromBottom  = "Draw From The Bottom";
        public const string Reverse         = "Reverse";

        //Combos
        public const string Pair            = "Pair";
        public const string ThreeOfAKind    = "Three Of A Kind";

        //Cat Cards
        public const string Tacocat         = "Tacocat";
        public const string MelonCat        = "Melon Cat"; //??
        public const string HairyPotatoCat  = "Hairy Potato Cat";
        public const string BeardCat        = "Beard Cat";
        public const string RainbowCat      = "Rainbow Cat"; //??

        //Expansion 1
        public const string ImplodingKitten = "Imploding Kitten";

        public static List<ExplodingKittensCard> StandardDeck() => new List<ExplodingKittensCard>(80)
        {
            new NopeCard(),
            new NopeCard(),
            new NopeCard(),
            new NopeCard(),
            new AttackCard(),
            new AttackCard(),
            new AttackCard(),
            new AttackCard(),
            new SkipCard(),
            new SkipCard(),
            new SkipCard(),
            new SkipCard(),
            new FavorCard(),
            new FavorCard(),
            new FavorCard(),
            new FavorCard(),
            new ShuffleCard(),
            new ShuffleCard(),
            new ShuffleCard(),
            new ShuffleCard(),
            new SeeTheFutureCard(),
            new SeeTheFutureCard(),
            new SeeTheFutureCard(),
            new SeeTheFutureCard(),
            new SeeTheFutureCard(),
            new CatCard(Tacocat),
            new CatCard(Tacocat),
            new CatCard(Tacocat),
            new CatCard(Tacocat),
            new CatCard(MelonCat),
            new CatCard(MelonCat),
            new CatCard(MelonCat),
            new CatCard(MelonCat),
            new CatCard(HairyPotatoCat),
            new CatCard(HairyPotatoCat),
            new CatCard(HairyPotatoCat),
            new CatCard(HairyPotatoCat),
            new CatCard(BeardCat),
            new CatCard(BeardCat),
            new CatCard(BeardCat),
            new CatCard(BeardCat),
            new CatCard(RainbowCat),
            new CatCard(RainbowCat),
            new CatCard(RainbowCat),
            new CatCard(RainbowCat)
        };
    }
}
