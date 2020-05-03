using System;
using System.ComponentModel.DataAnnotations;
using MechHisui.SecretHitler;

#nullable disable warnings
namespace MechHisui.Core
{
    public sealed class SecretHitlerTheme : ISecretHitlerTheme
    {
        [Key]
        public string Key { get; set; }

        public string President { get; set; }
        public string Presidency { get; set; }
        public string Chancellor { get; set; }
        public string Chancellorship { get; set; }
        public string Hitler { get; set; }
        public string Parliament { get; set; }
        public string FascistParty { get; set; }
        public string Fascist { get; set; }
        public string LiberalParty { get; set; }
        public string Liberal { get; set; }
        public string Policy { get; set; }
        public string Policies { get; set; }
        public string Yes { get; set; }
        public string No { get; set; }

        public string FirstStall { get; set; }
        public string SecondStall { get; set; }
        public string ThirdStall { get; set; }
        public string PeopleEnactedFormat { get; set; }
        public string PeopleStateFormat { get; set; }
        string ISecretHitlerTheme.ThePeopleEnacted(string party) => String.Format(PeopleEnactedFormat, party);
        string ISecretHitlerTheme.ThePeopleState(int stalls) => String.Format(PeopleStateFormat, stalls);

        public string LiberalsWin { get; set; }
        public string FascistsWin { get; set; }

        public string KillFormat { get; set; }
        string ISecretHitlerTheme.Kill(string player) => String.Format(KillFormat, player);

        public string NoKillFormat { get; set; }
        public string KilledFormat { get; set; }
        string ISecretHitlerTheme.HitlerNotKilled(string player) => String.Format(NoKillFormat, player);
        string ISecretHitlerTheme.HitlerWasKilled() => String.Format(KilledFormat, Hitler, LiberalParty);
    }
}
