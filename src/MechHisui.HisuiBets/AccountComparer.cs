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

#pragma warning disable CA1720 // Identifier contains type name
        public override int GetHashCode(IBankAccount obj)
        {
            return obj?.UserId.GetHashCode() ?? 0;
        }
#pragma warning restore CA1720 // Identifier contains type name
    }
}
