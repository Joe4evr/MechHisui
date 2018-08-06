using System;
using System.Diagnostics;
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

        [DebuggerStepThrough]
        public RequireGameStateAttribute(GameState state)
        {
            RequiredState = state;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var shservice = services.GetService<SecretHitlerService>();
            if (shservice != null)
            {
                var game = shservice.GetGameFromChannel(context.Channel);

                if (game != null)
                {
                    return (game.State == RequiredState)
                        ? Task.FromResult(PreconditionResult.FromSuccess())
                        : Task.FromResult(PreconditionResult.FromError("Cannot use command at this time."));
                }
                return Task.FromResult(PreconditionResult.FromError("No game active in this channel."));
            }
            return Task.FromResult(PreconditionResult.FromError($"Service {nameof(SecretHitlerService)} not found."));
        }
    }
}
