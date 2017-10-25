using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;
using MechHisui.ExplodingKittens.Cards;

namespace MechHisui.ExplodingKittens
{
    public sealed class ExKitPlayer : Player
    {
        internal int HandCount => _hand.Count;

        public bool HasExploded { get; private set; } = false;
        public bool IsAttacked { get; internal set; } = false;
        public bool IsFavored { get; internal set; } = false;

        private IList<ExplodingKitttensCard> _hand = new List<ExplodingKitttensCard>();

        public ExKitPlayer(IUser user, IMessageChannel channel) : base(user, channel)
        {
        }

        internal void AddToHand(ExplodingKitttensCard card) => _hand.Add(card);

        internal Task SendHand()
        {
            return SendMessageAsync($"You have:\n{String.Join("\n", _hand.Select((c, i) => $"{i}: {c.CardName}"))}");
        }

        public void Explode() => HasExploded = true;
    }
}
