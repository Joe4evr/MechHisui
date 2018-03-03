using Discord;

namespace MechHisui.Superfight.Models
{
    public sealed class PlayerVote
    {
        public SuperfightPlayer Voter { get; }
        public SuperfightPlayer VoteTarget { get; }

        public PlayerVote(SuperfightPlayer voter, SuperfightPlayer voteTarget)
        {
            Voter = voter;
            VoteTarget = voteTarget;
        }
    }
}
