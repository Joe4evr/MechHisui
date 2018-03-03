using System;

namespace MechHisui.HisuiBets
{
    public enum DonationResult
    {
        MiscError           = 0,
        DonationSuccess     = 1,
        DonorNotFound       = 2,
        RecipientNotFound   = 3,
        DonorNotEnoughMoney = 4
    }
}