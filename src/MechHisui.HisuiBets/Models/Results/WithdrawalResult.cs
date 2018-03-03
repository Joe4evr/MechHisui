using System;

namespace MechHisui.HisuiBets
{
    public enum WithdrawalResult
    {
        MiscError             = 0,
        WithdrawalSuccess     = 1,
        AccountNotFound       = 2,
        AccountNotEnoughMoney = 3
    }
}
