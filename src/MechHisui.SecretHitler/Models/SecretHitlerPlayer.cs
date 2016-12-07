using Discord;
using Discord.Addons.MpGame;

namespace MechHisui.SecretHitler.Models
{
    public sealed class SecretHitlerPlayer : Player
    {
        public string Party { get; }
        public string Role { get; }
        public bool IsAlive { get; internal set; } = true;

        public SecretHitlerPlayer(
            IUser user, IMessageChannel channel,
            string party, string role)
            : base(user, channel)
        {
            Party = party;
            Role = role;
        }
    }
}
