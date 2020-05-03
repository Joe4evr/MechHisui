using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;

namespace MechHisui.SecretHitler
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed class RequirePlayerRoleAttribute : PreconditionAttribute
    {
        private PlayerRole RequiredRole { get; }

        [DebuggerStepThrough]
        public RequirePlayerRoleAttribute(PlayerRole role)
        {
            RequiredRole = role;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var shservice = services.GetService<SecretHitlerService>();
            if (shservice is null)
                return Task.FromResult(PreconditionResult.FromError($"Service {nameof(SecretHitlerService)} not found."));

            var game = shservice.GetGameFromChannel(context.Channel);
            if (game is null)
                return Task.FromResult(PreconditionResult.FromError("No game active in this channel."));

            var authorId = context.User.Id;
            switch (RequiredRole)
            {
                case PlayerRole.President:
                    if (authorId == game.CurrentPresident.User.Id)
                    {
                        return Task.FromResult(PreconditionResult.FromSuccess());
                    }
                    else
                    {
                        goto default;
                    }
                case PlayerRole.Chancellor:
                    if (authorId == game.CurrentChancellor!.User.Id)
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
    }

    internal enum PlayerRole
    {
        President = 0,
        Chancellor = 1
    }
}
