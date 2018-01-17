using System;
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
                var game = shservice.GetGameFromChannel(context.Channel);

                if (game != null)
                {
                    var authorId = context.User.Id;
                    var presidentId = game.CurrentPresident.User.Id;
                    var chancellorId = game.CurrentChancellor.User.Id;

                    switch (RequiredRole)
                    {
                        case PlayerRole.President:
                            if (authorId == presidentId)
                            {
                                return Task.FromResult(PreconditionResult.FromSuccess());
                            }
                            else
                            {
                                goto default;
                            }
                        case PlayerRole.Chancellor:
                            if (authorId == chancellorId)
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
                return Task.FromResult(PreconditionResult.FromError("No game active in this channel."));
            }
            return Task.FromResult(PreconditionResult.FromError($"Service {nameof(SecretHitlerService)} not found."));
        }
    }

    internal enum PlayerRole
    {
        President = 0,
        Chancellor = 1
    }
}
