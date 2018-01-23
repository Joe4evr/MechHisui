using System;
using System.Collections.Generic;
using System.Text;

namespace MechHisui.HisuiBets
{
    internal sealed class AccountComparer : EqualityComparer<IBankAccount>
    {
        public static AccountComparer Instance { get; } = new AccountComparer();

        private AccountComparer() { }

        public override bool Equals(IBankAccount x, IBankAccount y)
        {
            return x?.UserId.GetHashCode() == y?.UserId.GetHashCode();
        }

        public override int GetHashCode(IBankAccount obj)
        {
            return obj?.UserId.GetHashCode() ?? 0;
        }
    }
}
