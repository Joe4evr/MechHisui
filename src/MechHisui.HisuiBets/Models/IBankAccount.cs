﻿using System;

namespace MechHisui.HisuiBets
{
    public interface IBankAccount
    {
        ulong UserId { get; }
        int Balance { get; }
    }
}
