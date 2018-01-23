using System;
using System.Collections.Generic;
using System.Text;

namespace MechHisui.HisuiBets
{
    public interface IBankAccount
    {
        ulong UserId { get; }
        int Bucks { get; }
    }
}
