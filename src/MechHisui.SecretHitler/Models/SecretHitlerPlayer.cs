using Discord;
using Discord.Addons.MpGame;

namespace MechHisui.SecretHitler.Models
{
    public sealed class SecretHitlerPlayer : Player
    {
        internal string Party { get; }
        internal string Role { get; }
        internal bool IsAlive { get; private set; } = true;

        public SecretHitlerPlayer(
            IUser user, IMessageChannel channel,
            string party, string role)
            : base(user, channel)
        {
            Party = party;
            Role = role;
        }

        internal void Killed() => IsAlive = false;
    }
}
