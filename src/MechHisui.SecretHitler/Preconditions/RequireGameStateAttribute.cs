using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using MechHisui.SecretHitler.Models;

namespace MechHisui.SecretHitler
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed class RequireGameStateAttribute : PreconditionAttribute
    {
        private GameState RequiredState { get; }
        public RequireGameStateAttribute(GameState state)
        {
            RequiredState = state;
        }

        public async override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var shservice = services.GetService<SecretHitlerService>();
            if (shservice != null)
            {
                var game = await shservice.GetGameFromChannelAsync(context.Channel).ConfigureAwait(false);

                if (game != null)
                {
                    return (game.State == RequiredState)
                        ? PreconditionResult.FromSuccess()
                        : PreconditionResult.FromError("Cannot use command at this time.");
                }
                return PreconditionResult.FromError("No game.");
            }
            return PreconditionResult.FromError("No service.");
        }
    }
}
