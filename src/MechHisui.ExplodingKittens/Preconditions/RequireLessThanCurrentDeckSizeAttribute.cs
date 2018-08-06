using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;

namespace MechHisui.ExplodingKittens
{
    internal sealed class RequireLessThanCurrentDeckSizeAttribute : ParameterPreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context,
            ParameterInfo parameter,
            object value,
            IServiceProvider services)
        {
            if (value is uint location)
            {
                var exkservice = services.GetService<ExKitService>();
                if (exkservice != null)
                {
                    var game = exkservice.GetGameFromChannel(context.Channel);

                    if (game != null)
                    {
                        return (location <= game.DeckSize)
                            ? Task.FromResult(PreconditionResult.FromSuccess())
                            : Task.FromResult(PreconditionResult.FromError("Location out of range."));
                    }
                    return Task.FromResult(PreconditionResult.FromError("No game active in this channel."));
                }
                return Task.FromResult(PreconditionResult.FromError($"Service '{nameof(ExKitService)}' not found."));
            }
            return Task.FromResult(PreconditionResult.FromError("Argument not a valid integer."));
        }
    }
}
