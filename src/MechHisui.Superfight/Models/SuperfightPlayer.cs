using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;

namespace MechHisui.Superfight.Models
{
    public sealed class SuperfightPlayer : Player
    {
        public int Points { get; private set; } = 0;
        internal int HandSize => _hand.Count;
        internal List<Card> Tentative { get; } = new List<Card>();
        internal bool ConfirmedPlay { get; set; } = false;

        private readonly List<Card> _hand = new List<Card>();

        public SuperfightPlayer(IUser user, IMessageChannel channel) : base(user, channel)
        {
        }

        internal void Draw(Card card)
        {
            Tentative.Clear();
            _hand.Add(card);
        }

        public Task SendHand()
        {
            ConfirmedPlay = false;
            int i = 1;
            var sb = new StringBuilder("Your Hand:\n")
                .AppendSequence(_hand, (b, c) => b.AppendLine($"[{i++}]: **{c.Type}** - {c.Text}"))
                .Append($"Please pick one {CardType.Character} and one {CardType.Ability} card.");

            return SendMessageAsync(sb.ToString());
        }

        public string ChooseCard(int i)
        {
            var tc = _hand[i - 1];
            if (Tentative.Any(c => c.Type == tc.Type))
            {
                Tentative.RemoveAll(c => c.Type == tc.Type);
                return $"Replacing your tentative {tc.Type} card to `{tc.Text}`";
            }
            else
            {
                Tentative.Add(tc);
                return $"Added {tc.Type}: `{tc.Text}` to tentative play.";
            }
        }

        internal List<Card> Confirm()
        {
            ConfirmedPlay = true;
            _hand.RemoveAll(c => Tentative.Select(t => t.Text).Contains(c.Text));
            return Tentative;
        }

        public void AddPoint() => Points++;
    }
}
