using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;
using Discord.Addons.MpGame.Collections;
using SharedExtensions;
using MechHisui.ExplodingKittens.Cards;

namespace MechHisui.ExplodingKittens
{
    public sealed class ExKitPlayer : Player
    {
        internal int HandCount => _hand.Count;

        public bool HasExploded { get; private set; } = false;
        public bool IsAttacked { get; internal set; } = false;
        public bool IsFavored { get; internal set; } = false;

        private Hand<ExplodingKittensCard> _hand = new Hand<ExplodingKittensCard>();

        public ExKitPlayer(IUser user, IMessageChannel channel)
            : base(user, channel)
        {
        }

        internal void AddToHand(ExplodingKittensCard card)
            => _hand.Add(card);

        internal Task SendHand()
            => SendMessageAsync($"You have:\n{String.Join("\n", _hand.Browse().Select((c, i) => $"{i}: {c.CardName}"))}");

        internal void Explode()
            => HasExploded = true;

        internal ExplodingKittensCard TakeCard(int index)
            => _hand.TakeAt(index);

        internal ExplodingKittensCard TakeCard(Type type)
            => _hand.TakeFirstOrDefault(c => c.GetType() == type);

        internal TCard TakeCard<TCard>()
            where TCard : ExplodingKittensCard
            => (TCard)TakeCard(typeof(TCard));

        //internal DefuseCard TakeDefuse()
        //{
        //    return (DefuseCard)_hand.TakeFirstOrDefault(c => c is DefuseCard);
        //}
    }
}
