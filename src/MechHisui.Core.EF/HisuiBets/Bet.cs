using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MechHisui.HisuiBets;

namespace MechHisui.Core
{
    public sealed class Bet : IBet
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public BetGame BetGame  { get; set; }
        public HisuiUser User   { get; set; }
        public string UserName  { get; set; }
        public string Target    { get; set; }
        public int BettedAmount { get; set; }

        int IBet.BetGameId => BetGame.Id;
        ulong IBet.UserId => User.UserId;
    }
}
