using System;

namespace MechHisui.HisuiBets
{
    public struct WithdrawalRequest
    {
        public WithdrawalRequest(uint amount, ulong accountId)
        {
            Amount = amount;
            AccountId = accountId;
        }

        public uint Amount { get; }
        public ulong AccountId { get; }
    }
}
