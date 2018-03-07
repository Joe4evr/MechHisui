namespace MechHisui.SecretHitler.Models
{
    internal sealed class PolicyCard
    {
        public PolicyType PolicyType { get; }

        public PolicyCard(PolicyType policyType)
        {
            PolicyType = policyType;
        }
    }

    internal enum PolicyType
    {
        Liberal = 1,
        Fascist = 2
    }
}
