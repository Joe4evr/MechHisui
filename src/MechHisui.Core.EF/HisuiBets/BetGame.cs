using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MechHisui.HisuiBets;

namespace MechHisui.Core
{
    public sealed class BetGame : IBetGame
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public HisuiChannel Channel  { get; set; }
        public bool IsCollected      { get; set; }
        public bool IsCashedOut      { get; set; }
        public GameType GameType     { get; set; }
        public IEnumerable<Bet> Bets { get; set; }

        ulong IBetGame.ChannelId => Channel.ChannelId;
        IEnumerable<IBet> IBetGame.Bets => Bets;
    }
}
