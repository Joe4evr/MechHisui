using System;

namespace MechHisui.HisuiBets
{
    internal sealed class UserAccount : IBankAccount
    {
        public ulong UserId { get; set; }
        public int Bucks { get; set; }
    }
}
