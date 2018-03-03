namespace MechHisui.Superfight
{
    public interface ISuperfightCard
    {
        CardType Type { get; }
        string Text { get; }
    }

    internal sealed class CharacterCard : ISuperfightCard
    {
        public string Text { get; }
        CardType ISuperfightCard.Type => CardType.Character;

        public CharacterCard(string text)
        {
            Text = text;
        }
    }

    internal sealed class AbilityCard : ISuperfightCard
    {
        public string Text { get; }
        CardType ISuperfightCard.Type => CardType.Ability;

        public AbilityCard(string text)
        {
            Text = text;
        }
    }

    internal sealed class LocationCard : ISuperfightCard
    {
        public string Text { get; }
        CardType ISuperfightCard.Type => CardType.Location;

        public LocationCard(string text)
        {
            Text = text;
        }
    }
}
