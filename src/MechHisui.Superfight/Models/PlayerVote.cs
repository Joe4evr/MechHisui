using Discord;

namespace MechHisui.Superfight.Models
{
    public sealed class PlayerVote
    {
        public IUser Voter { get; }
        public SuperfightPlayer VoteTarget { get; }

        public PlayerVote(IUser voter, SuperfightPlayer voteTarget)
        {
            Voter = voter;
            VoteTarget = voteTarget;
        }
    }
}
