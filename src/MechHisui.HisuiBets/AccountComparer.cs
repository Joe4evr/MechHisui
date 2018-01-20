using System;
using System.Collections.Generic;
using System.Text;

namespace MechHisui.HisuiBets
{
    internal sealed class AccountComparer : EqualityComparer<UserAccount>
    {
        public static AccountComparer Instance { get; } = new AccountComparer();

        private AccountComparer() { }

        public override bool Equals(UserAccount x, UserAccount y)
        {
            return x?.UserId.GetHashCode() == y?.UserId.GetHashCode();
        }

        public override int GetHashCode(UserAccount obj)
        {
            return obj.UserId.GetHashCode();
        }
    }
}
