using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace MechHisui.Superfight.Preconditions
{

    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class RequirePlayerRoleAttribute : PreconditionAttribute
    {
        private PlayerRole Role { get; }

        internal RequirePlayerRoleAttribute(PlayerRole role)
        {
            Role = role;
        }

        public override Task<PreconditionResult> CheckPermissions(CommandContext context, CommandInfo command, IDependencyMap map)
        {
            var service = map.Get<SuperfightService>();
            if (service != null)
            {
                var authorId = context.User.Id;
                var game = service.GameList.Values
                    .Where(g => g.PlayerChannels.Select(c => c.Id).Contains(context.Channel.Id))
                    .FirstOrDefault();
                if (game != null || service.GameList.TryGetValue(context.Channel.Id, out game))
                {
                    var fighter1 = game.TurnPlayers[0];
                    var fighter2 = game.TurnPlayers[1];

                    switch (Role)
                    {
                        case PlayerRole.Fighter:
                            return (fighter1.User.Id == context.User.Id || fighter2.User.Id == context.User.Id)
                                ? Task.FromResult(PreconditionResult.FromSuccess())
                                : Task.FromResult(PreconditionResult.FromError("Cannot use command at this time."));
                        case PlayerRole.NonFighter:
                            return !(fighter1.User.Id == context.User.Id || fighter2.User.Id == context.User.Id)
                                ? Task.FromResult(PreconditionResult.FromSuccess())
                                : Task.FromResult(PreconditionResult.FromError("Cannot use command at this time."));
                    }
                }
                return Task.FromResult(PreconditionResult.FromError("No game."));
            }
            return Task.FromResult(PreconditionResult.FromError("No service."));
        }
    }

    internal enum PlayerRole
    {
        Fighter,
        NonFighter
    }
}
