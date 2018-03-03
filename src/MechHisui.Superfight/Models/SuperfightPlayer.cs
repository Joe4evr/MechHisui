using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;
using SharedExtensions;

namespace MechHisui.Superfight.Models
{
    public sealed class SuperfightPlayer : Player
    {
        public int Points { get; private set; } = 0;
        internal int HandSize => _hand.Count;
        internal Dictionary<CardType, ISuperfightCard> Tentative { get; } = new Dictionary<CardType, ISuperfightCard>();
        internal bool ConfirmedPlay { get; set; } = false;

        private readonly List<ISuperfightCard> _hand = new List<ISuperfightCard>();

        public SuperfightPlayer(IUser user, IMessageChannel channel)
            : base(user, channel)
        {
        }

        internal void Draw(ISuperfightCard card)
        {
            Tentative.Clear();
            _hand.Add(card);
        }

        public Task SendHand()
        {
            ConfirmedPlay = false;
            var sb = new StringBuilder("Your Hand:\n")
                .AppendLine(String.Join("\n", _hand.Select((c, i) => $"[{i+1}]: **{c.Type}** - {c.Text}")))
                .Append($"Please pick one {CardType.Character} and one {CardType.Ability} card.");

            return SendMessageAsync(sb.ToString());
        }

        public string ChooseCard(int i)
        {
            var tc = _hand[i - 1];
            var result = (Tentative.TryGetValue(tc.Type, out var card))
                ? $"Replacing your tentative **{tc.Type}** card from `{card.Text}` to `{tc.Text}`"
                : $"Added **{tc.Type}**: `{tc.Text}` to tentative play.";

            Tentative[tc.Type] = tc;
            return result;
        }

        internal IReadOnlyList<ISuperfightCard> Confirm()
        {
            ConfirmedPlay = true;
            _hand.RemoveAll(c => Tentative.Select(t => t.Value.Text).Contains(c.Text));
            return Tentative.Select(t => t.Value).ToList();
        }

        public void AddPoint() => Points++;
    }
}
