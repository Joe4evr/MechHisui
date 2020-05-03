using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;

namespace MechHisui.ExplodingKittens
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal sealed class RequirePlayerAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var exkservice = services.GetService<ExKitService>();
            if (exkservice is null)
                return Task.FromResult(PreconditionResult.FromError($"Service '{nameof(ExKitService)}' not found."));

            var game = exkservice.GetGameFromChannel(context.Channel);
            if (game is null)
                return Task.FromResult(PreconditionResult.FromError("No game active in this channel."));

            var authorId = context.User.Id;
            return (game.PlayerIds.Contains(authorId))
                ? Task.FromResult(PreconditionResult.FromSuccess())
                : Task.FromResult(PreconditionResult.FromError("Command can only be used by a player."));
        }
    }
}
