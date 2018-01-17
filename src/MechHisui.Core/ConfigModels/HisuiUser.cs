using System;
using Discord.Addons.SimplePermissions;
using MechHisui.FateGOLib;

namespace MechHisui.Core
{
    public class HisuiUser : ConfigUser
    {
        public int BankBalance { get; set; } = 100;

        public FgoFriendData FriendData { get; set; }
    }
}
