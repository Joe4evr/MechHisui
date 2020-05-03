﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;

namespace MechHisui.ExplodingKittens
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal sealed class RequireTurnPlayerAttribute : PreconditionAttribute
    {
        private readonly bool _equalingTurnPlayer;

        public RequireTurnPlayerAttribute(bool equalingTurnPlayer)
        {
            _equalingTurnPlayer = equalingTurnPlayer;
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

            var authorId = context.User.Id;
            var turnPlayerId = game.TurnPlayer.Value.User.Id;

            return ((authorId == turnPlayerId) == _equalingTurnPlayer)
                ? Task.FromResult(PreconditionResult.FromSuccess())
                : Task.FromResult(PreconditionResult.FromError("Cannot use command at this time."));
        }
    }
}
