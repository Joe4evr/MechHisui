﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;

namespace MechHisui.SecretHitler
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed class RequirePlayerRoleAttribute : PreconditionAttribute
    {
        private PlayerRole RequiredRole { get; }

        public RequirePlayerRoleAttribute(PlayerRole role)
        {
            RequiredRole = role;
        }

        public async override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var shservice = services.GetService<SecretHitlerService>();
            if (shservice != null)
            {
                var authorId = context.User.Id;
                var game = await shservice.GetGameFromChannelAsync(context.Channel).ConfigureAwait(false);

                if (game != null)
                {
                    var president = game.CurrentPresident;
                    var chancellor = game.CurrentChancellor;

                    switch (RequiredRole)
                    {
                        case PlayerRole.President:
                            if (authorId == president.User.Id)
                            {
                                return PreconditionResult.FromSuccess();
                            }
                            else
                            {
                                goto default;
                            }
                        case PlayerRole.Chancellor:
                            if (authorId == chancellor.User.Id)
                            {
                                return PreconditionResult.FromSuccess();
                            }
                            else
                            {
                                goto default;
                            }
                        default:
                            return PreconditionResult.FromError("Cannot use command at this time.");
                    }
                }
                return PreconditionResult.FromError("No game.");
            }
            return PreconditionResult.FromError("No service.");
        }
    }

    internal enum PlayerRole
    {
        President = 0,
        Chancellor = 1
    }
}
