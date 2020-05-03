using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;

namespace MechHisui.SecretHitler
{
    internal sealed class RequireLivingPlayerAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var service = services.GetService<SecretHitlerService>();
            if (service is null)
                return Task.FromResult(PreconditionResult.FromError("Required service not found."));

            var game = service.GetGameFromChannel(context.Channel);
            if (game is null)
                return Task.FromResult(PreconditionResult.FromError("No game active."));

            var authorId = context.User.Id;
            return (game.LivingPlayers.Select(p => p.User.Id).Contains(authorId))
                ? Task.FromResult(PreconditionResult.FromSuccess())
                : Task.FromResult(PreconditionResult.FromError("User must be a Player in this game."));
        }
    }
}
