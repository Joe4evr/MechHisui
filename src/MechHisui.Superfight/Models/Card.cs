namespace MechHisui.Superfight.Models
{
    internal sealed class Card
    {
        public CardType Type { get; }
        public string Text { get; }

        public Card(CardType type, string text)
        {
            Type = type;
            Text = text;
        }
    }
}
