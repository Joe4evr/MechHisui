using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;

namespace MechHisui.Superfight.Preconditions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed class RequirePlayerRoleAttribute : PreconditionAttribute
    {
        private PlayerRole Role { get; }

        internal RequirePlayerRoleAttribute(PlayerRole role)
        {
            Role = role;
        }

        public async override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var sfservice = services.GetService<SuperfightService>();
            if (sfservice != null)
            {
                var authorId = context.User.Id;
                var game = await sfservice.GetGameFromChannelAsync(context.Channel).ConfigureAwait(false);

                if (game != null)
                {
                    var fighter1 = game.TurnPlayers[0];
                    var fighter2 = game.TurnPlayers[1];

                    switch (Role)
                    {
                        case PlayerRole.Fighter:
                            return (fighter1.User.Id == context.User.Id || fighter2.User.Id == context.User.Id)
                                ? PreconditionResult.FromSuccess()
                                : PreconditionResult.FromError("Cannot use command at this time.");
                        case PlayerRole.NonFighter:
                            return !(fighter1.User.Id == context.User.Id || fighter2.User.Id == context.User.Id)
                                ? PreconditionResult.FromSuccess()
                                : PreconditionResult.FromError("Cannot use command at this time.");
                    }
                }
                return PreconditionResult.FromError("No game.");
            }
            return PreconditionResult.FromError("No service.");
        }
    }

    internal enum PlayerRole
    {
        NonFighter = 0,
        Fighter = 1
    }
}
