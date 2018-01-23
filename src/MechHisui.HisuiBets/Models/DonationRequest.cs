using System;

namespace MechHisui.HisuiBets
{
    public struct DonationRequest
    {
        public DonationRequest(uint amount, ulong donorId, ulong recepientId)
        {
            Amount = amount;
            DonorId = donorId;
            RecepientId = recepientId;
        }

        public uint Amount { get; }
        public ulong DonorId { get; }
        public ulong RecepientId { get; }
    }
}
