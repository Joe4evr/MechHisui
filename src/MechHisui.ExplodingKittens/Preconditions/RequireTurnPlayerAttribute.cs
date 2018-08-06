using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;

namespace MechHisui.ExplodingKittens
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal sealed class RequireTurnPlayerAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context,
            CommandInfo command,
            IServiceProvider services)
        {
            var exkservice = services.GetService<ExKitService>();
            if (exkservice != null)
            {
                var game = exkservice.GetGameFromChannel(context.Channel);

                if (game != null)
                {
                    var authorId = context.User.Id;
                    var turnPlayerId = game.TurnPlayer.Value.User.Id;

                    return (authorId == turnPlayerId) 
                        ? Task.FromResult(PreconditionResult.FromSuccess())
                        : Task.FromResult(PreconditionResult.FromError("Cannot use command at this time."));
                }
                return Task.FromResult(PreconditionResult.FromError("No game active in this channel."));
            }
            return Task.FromResult(PreconditionResult.FromError($"Service '{nameof(ExKitService)}' not found."));
        }
    }
}
