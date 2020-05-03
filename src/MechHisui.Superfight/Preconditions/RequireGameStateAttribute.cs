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

        public override Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var sfservice = services.GetService<SuperfightService>();
            if (sfservice is null)
                return Task.FromResult(PreconditionResult.FromError("No service."));

            var game = sfservice.GetGameFromChannel(context.Channel);
            if (game is null)
                return Task.FromResult(PreconditionResult.FromError("No game."));

            return (game.State == RequiredState)
                ? Task.FromResult(PreconditionResult.FromSuccess())
                : Task.FromResult(PreconditionResult.FromError("Cannot use command at this time."));
        }
    }
}
