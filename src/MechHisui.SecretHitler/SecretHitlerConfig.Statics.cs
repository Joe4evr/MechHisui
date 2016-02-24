namespace MechHisui.SecretHitler
{
    public partial class SecretHitlerConfig
    {
        public static SecretHitlerConfig Default = new SecretHitlerConfig();

        public static SecretHitlerConfig AngryManjew = new SecretHitlerConfig
        {
            President = "Director",
            //Presidency = "Directorship",
            Chancellor = "High-priest",
            //Chancellorship = "High-priesthood",
            Hitler = "Angry Manjew",
            Parliament = "",
            FascistParty = "Holy Church",
            Fascist = "Priest",
            LiberalParty = "Mages Association",
            Liberal = "Magus",
            Policy = "Sacrament",
            Policies = "Sacraments",
            Yes = "Hai",
            No = "Dame",
            Kill = "The Director has shot **{0}** with an Origin Bullet.",
            //ThePeopleOne = "The Nobles are disappointed.",
            //ThePeopleTwo = "The Nobles are upset.",
            //ThePeopleThree = "The Nobles are issueing their own Sacrament.",
            //ThePeopleEnacted = "The Nobles have issued a **{0}** Sacrament",
            FascistsWin = "",
            LiberalsWin = "",
            HitlerNotKilled = "",
            HitlerWasKilled = ""
        };

        public static SecretHitlerConfig JojosBizarreAdventure = new SecretHitlerConfig
        {
            President = "CEO",
            //Presidency = "",
            Chancellor = "Chairman",
            //Chancellorship = "",
            Hitler = "DIO",
            Parliament = "The party",
            FascistParty = "Vampires",
            Fascist = "Vampiric",
            LiberalParty = "Stardust Crusaders",
            Liberal = "Stardust",
            Policy = "Win",
            Policies = "Wins",
            Yes = "ORA",
            No = "MUDA",
            Kill = "The CEO has blasted a Hamon through **{0}**'s head.",
            ThePeopleOne = "The SPWF Employees have become disheartened.",
            ThePeopleTwo = "The SPWF Employees are rioting.",
            ThePeopleThree = "The SPWF Employees are causing a fight on their own.",
            ThePeopleEnacted = "The SPWF Employees have caused a **{0}** Win.",
            FascistsWin = "WRYYYYYYYYYYYYYYYYYYYY!!!!!!!!",
            LiberalsWin = "Yare yare, daze.",
            HitlerNotKilled = "GOODBYE JOJO!",
            HitlerWasKilled = "DIO has been killed. The Stardust Crusaders are victorious."
        };
    }
}
