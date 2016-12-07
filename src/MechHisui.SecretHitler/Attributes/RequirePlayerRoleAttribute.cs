﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace MechHisui.SecretHitler
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class RequirePlayerRoleAttribute : PreconditionAttribute
    {
        private PlayerRole Role { get; }

        public RequirePlayerRoleAttribute(PlayerRole role)
        {
            Role = role;
        }

        public override Task<PreconditionResult> CheckPermissions(CommandContext context, CommandInfo command, IDependencyMap map)
        {
            var service = map.Get<SecretHitlerService>();
            if (service != null)
            {
                var authorId = context.User.Id;
                var game = service.GameList.Values
                    .Where(g => g.PlayerChannels.Select(c => c.Id).Contains(context.Channel.Id))
                    .FirstOrDefault();
                if (game != null || service.GameList.TryGetValue(context.Channel.Id, out game))
                {
                    var president = game.CurrentPresident.Value;
                    var chancellor = game.CurrentChancellor;

                    switch (Role)
                    {
                        case PlayerRole.President:
                            if (authorId == president.User.Id)
                                return Task.FromResult(PreconditionResult.FromSuccess());
                            else
                                goto default;
                        case PlayerRole.Chancellor:
                            if (authorId == chancellor.User.Id)
                                return Task.FromResult(PreconditionResult.FromSuccess());
                            else
                                goto default;
                        default:
                            return Task.FromResult(PreconditionResult.FromError("Cannot use command at this time."));
                    }
                }
                return Task.FromResult(PreconditionResult.FromError("No game."));
            }
            return Task.FromResult(PreconditionResult.FromError("No service."));
        }
    }

    internal enum PlayerRole
    {
        President,
        Chancellor
    }
}
