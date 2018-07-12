using System;

namespace MechHisui.HisuiBets
{
    public readonly struct WithdrawalRequest
    {
        internal WithdrawalRequest(int amount, ulong accountId)
        {
            Amount = amount;
            AccountId = accountId;
        }

        public int Amount { get; }
        public ulong AccountId { get; }
    }
}
