using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MechHisui.HisuiBets;

namespace MechHisui.Core
{
    public sealed class PreliminaryBet : IBet
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public HisuiChannel Channel { get; set; }
        public HisuiUser User       { get; set; }
        public GameType GameType    { get; set; }
        public string UserName      { get; set; }
        public string Target        { get; set; }
        public int BettedAmount     { get; set; }

        ulong IBet.ChannelId => Channel.ChannelId;
        ulong IBet.UserId => User.UserId;
        uint IBet.BettedAmount => (uint)BettedAmount;
    }
}
