﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MechHisui.FateGOLib;

#nullable disable warnings
namespace MechHisui.Core
{
    public sealed partial class HisuiUser : IFgoFriendData
    {
        public string FriendCode { get; set; }
    }
    //public sealed class UserAP
    //{
    //    public int StartAP { get; set; }
    //    public ulong UserID { get; set; }
    //    public TimeSpan StartTimeLeft { get; set; }
    //    public DateTime StartTime { get; } = DateTime.UtcNow;
    //    //public int CurrentAP => StartAP + (int)Math.Floor(
    //    //    (DateTime.UtcNow - (StartTime - StartTimeLeft))
    //    //    .TotalMinutes / FgoHelpers.PerAP.TotalMinutes);
    //}


    //public sealed class FgoFriendData : IFgoFriendData
    //{
    //    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    //    public int Id { get; set; }

    //    public string FriendCode { get; set; }
    //    //public string Class { get; set; }
    //    //public string Servant { get; set; }

    //    //public HisuiUser User { get; set; }
    //    //public int UserFK { get; set; }
    //}
}
