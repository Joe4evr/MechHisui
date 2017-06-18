using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;

namespace MechHisui.SecretHitler
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed class RequirePlayerRoleAttribute : PreconditionAttribute
    {
        private PlayerRole RequiredRole { get; }

        public RequirePlayerRoleAttribute(PlayerRole role)
        {
            RequiredRole = role;
        }

        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var shservice = services.GetService<SecretHitlerService>();
            if (shservice != null)
            {
                var authorId = context.User.Id;
                var game = shservice.GameList.Values
                    .FirstOrDefault(g => g.PlayerChannels.Select(c => c.Id).Contains(context.Channel.Id))
;

                if (game != null || shservice.GameList.TryGetValue(context.Channel, out game))
                {
                    var president = game.CurrentPresident;
                    var chancellor = game.CurrentChancellor;

                    switch (RequiredRole)
                    {
                        case PlayerRole.President:
                            if (authorId == president.User.Id)
                            {
                                return Task.FromResult(PreconditionResult.FromSuccess());
                            }
                            else
                            {
                                goto default;
                            }
                        case PlayerRole.Chancellor:
                            if (authorId == chancellor.User.Id)
                            {
                                return Task.FromResult(PreconditionResult.FromSuccess());
                            }
                            else
                            {
                                goto default;
                            }
                        default:
                            return Task.FromResult(PreconditionResult.FromError("Cannot use command at this time."));
                    }
                }
                return Task.FromResult(PreconditionResult.FromError("No game."));
            }
            return Task.FromResult(PreconditionResult.FromError("No service."));
        }
    }

    internal enum PlayerRole
    {
        President = 0,
        Chancellor = 1
    }
}
