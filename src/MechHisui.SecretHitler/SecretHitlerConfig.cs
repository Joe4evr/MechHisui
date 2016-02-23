namespace MechHisui.SecretHitler
{
    public partial class SecretHitlerConfig
    {
        public string President { get; set; } = "President";
        public string Presidency { get; set; } = "Presidency";
        public string Chancellor { get; set; } = "Chancellor";
        public string Chancellorship { get; set; } = "Chancellorship";
        public string Hitler { get; set; } = "Hitler";
        public string Parliament { get; set; } = "Parliament";
        public string FascistParty { get; set; } = "Fascist Party";
        public string Fascist { get; set; } = "Fascist";
        public string LiberalParty { get; set; } = "Liberal Party";
        public string Liberal { get; set; } = "Liberal";
        public string Policy { get; set; } = "Policy";
        public string Policies { get; set; } = "Policies";
        public string Yes { get; set; } = "Ja";
        public string No { get; set; } = "Nein";
        public string Bullet { get; set; } = "Bullet";
        public string ThePeople { get; set; } = "The people";
        public string LiberalsWin { get; set; } = "The Liberals have won. Freedom reigns supreme.";
        public string FascistsWin { get; set; } = "The Fascists have won. Hitler has taken over.";
        public string HitlerNotKilled(string name) => $"**{name}** was not {Hitler}. The game proceeds as normal.";
        public string HitlerWasKilled() => $"**{Hitler}** has been eliminated. **{LiberalParty}** wins.";
    }
}
