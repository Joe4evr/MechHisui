namespace MechHisui.SecretHitler
{
    internal class DefaultSecretHitlerTheme : ISecretHitlerTheme
    {
        public static readonly ISecretHitlerTheme Instance = new DefaultSecretHitlerTheme();

        private DefaultSecretHitlerTheme() { }

        public string President      { get; } = nameof(President);
        public string Presidency     { get; } = nameof(Presidency);
        public string Chancellor     { get; } = nameof(Chancellor);
        public string Chancellorship { get; } = nameof(Chancellorship);
        public string Hitler         { get; } = nameof(Hitler);
        public string Parliament     { get; } = nameof(Parliament);
        public string FascistParty   { get; } = "Fascist Party";
        public string Fascist        { get; } = nameof(Fascist);
        public string LiberalParty   { get; } = "Liberal Party";
        public string Liberal        { get; } = nameof(Liberal);
        public string Policy         { get; } = nameof(Policy);
        public string Policies       { get; } = nameof(Policies);
        public string Yes            { get; } = "Ja";
        public string No             { get; } = "Nein";

        public string FirstStall     { get; } = "The People are disappointed.";
        public string SecondStall    { get; } = "The People are upset.";
        public string ThirdStall     { get; } = "The People are enacting their own Policy.";

        public string ThePeopleEnacted(string party) => $"The People have enacted a **{party}** Policy";
        public string ThePeopleState(int stalls) => $"The People are {stalls} stalls away from enacting their own Policy.";

        public string Kill(string player) => $"The President has formally executed **{player}**.";
        public string HitlerNotKilled(string player) => $"**{player}** was not {Hitler}. The game proceeds as normal.";
        public string HitlerWasKilled() => $"**{Hitler}** has been eliminated. The **{LiberalParty}** wins.";

        public string LiberalsWin { get; } = "The Liberals have won. Freedom reigns supreme.";
        public string FascistsWin { get; } = "The Fascists have won. Hitler has taken over.";
    }
}
