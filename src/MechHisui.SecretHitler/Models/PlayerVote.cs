using Discord;

namespace MechHisui.SecretHitler.Models
{
    internal sealed class PlayerVote
    {
        public IUser User { get; }
        public Vote Vote { get; }

        public PlayerVote(IUser user, Vote vote)
        {
            User = user;
            Vote = vote;
        }
    }
}
