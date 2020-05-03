using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;

namespace MechHisui.ExplodingKittens
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed class RequireGameStateAttribute : PreconditionAttribute
    {
        private GameState RequiredState { get; }
        public RequireGameStateAttribute(GameState state)
        {
            RequiredState = state;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var exkservice = services.GetService<ExKitService>();
            if (exkservice is null)
                return Task.FromResult(PreconditionResult.FromError($"Service '{nameof(ExKitService)}' not found."));

            var game = exkservice.GetGameFromChannel(context.Channel);
            if (game is null)
                return Task.FromResult(PreconditionResult.FromError("No game active in this channel."));

            return (game.State == RequiredState)
                ? Task.FromResult(PreconditionResult.FromSuccess())
                : Task.FromResult(PreconditionResult.FromError("Cannot use command at this time."));
        }
    }
}
