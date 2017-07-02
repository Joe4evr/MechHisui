using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using MechHisui.Superfight.Models;

namespace MechHisui.Superfight.Preconditions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed class RequireGameStateAttribute : PreconditionAttribute
    {
        private GameState RequiredState { get; }
        internal RequireGameStateAttribute(GameState state)
        {
            RequiredState = state;
        }

        public async override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var sfservice = services.GetService<SuperfightService>();
            if (sfservice != null)
            {
                var game = await sfservice.GetGameFromChannelAsync(context.Channel).ConfigureAwait(false);

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
