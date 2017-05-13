namespace MechHisui.SecretHitler
{
    public class SecretHitlerConfig
    {
        public static SecretHitlerConfig Default = new SecretHitlerConfig();

        public string Key { get; set; } = "default";
        public string President { get; set; } = "President";
        //public string Presidency { get; set; } = "Presidency";
        public string Chancellor { get; set; } = "Chancellor";
        //public string Chancellorship { get; set; } = "Chancellorship";
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
        public string ThePeopleState { get; set; } = "The People are {0} stalls away from enacting their own Policy.";
        public string ThePeopleOne { get; set; } = "The People are disappointed.";
        public string ThePeopleTwo { get; set; } = "The People are upset.";
        public string ThePeopleThree { get; set; } = "The People are enacting their own Policy.";
        public string ThePeopleEnacted { get; set; } = "The People have enacted a **{0}** Policy";
        public string LiberalsWin { get; set; } = "The Liberals have won. Freedom reigns supreme.";
        public string FascistsWin { get; set; } = "The Fascists have won. Hitler has taken over.";
        public string Kill { get; set; } = "The President has formally executed **{0}**.";
        public string HitlerNotKilled { get; set; } = "**{0}** was not {1}. The game proceeds as normal.";
        public string HitlerWasKilled { get; set; } = "**{0}** has been eliminated. **{1}** wins.";
    }
}
