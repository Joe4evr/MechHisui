using System;
using Discord;

namespace MechHisui.HisuiBets
{
    public struct DonationRequest
    {
        internal DonationRequest(uint amount, IBankAccount donor, IUser recepient)
        {
            Amount = amount;
            DonorId = donor.UserId;
            RecepientId = recepient.Id;
        }

        public uint Amount { get; }
        public ulong DonorId { get; }
        public ulong RecepientId { get; }
    }
}
