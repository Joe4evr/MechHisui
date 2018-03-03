using System;
using Discord.Addons.SimplePermissions;

namespace MechHisui.Core
{
    public class HisuiUser : ConfigUser
    {
        public int BankBalance { get; set; } = 1500;

        public FgoFriendData FriendData { get; set; }
    }
}
