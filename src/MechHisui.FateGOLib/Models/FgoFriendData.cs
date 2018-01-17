using System;

namespace MechHisui.FateGOLib
{
    //public class UserAP
    //{
    //    public int StartAP { get; set; }
    //    public ulong UserID { get; set; }
    //    public TimeSpan StartTimeLeft { get; set; }
    //    public DateTime StartTime { get; } = DateTime.UtcNow;
    //    //public int CurrentAP => StartAP + (int)Math.Floor(
    //    //    (DateTime.UtcNow - (StartTime - StartTimeLeft))
    //    //    .TotalMinutes / FgoHelpers.PerAP.TotalMinutes);
    //}

    public class FgoFriendData
    {
        public int Id { get; set; }
        //public ulong User { get; set; }
        public string FriendCode { get; set; }
        public string Class { get; set; }
        public string Servant { get; set; }
    }
}
