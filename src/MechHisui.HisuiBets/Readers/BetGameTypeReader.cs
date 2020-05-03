using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;

namespace MechHisui.HisuiBets
{
    public sealed partial class HisuiBetsModule
    {
        private sealed class BetGameTypeReader : TypeReader
        {
            public override async Task<TypeReaderResult> ReadAsync(
                ICommandContext context, string input, IServiceProvider services)
            {
                if (!Int32.TryParse(input, out int gameId))
                    return TypeReaderResult.FromError(CommandError.ParseFailed, "Could not parse input as integer.");

                var svc = services.GetService<HisuiBankService>();
                if (svc == null)
                    return TypeReaderResult.FromError(CommandError.ObjectNotFound, $"Could not find service: {nameof(HisuiBankService)}");

                var game = await svc.Bank.GetGameInChannelByIdAsync(context.Channel, gameId).ConfigureAwait(false);

                return (game == null)
                    ? TypeReaderResult.FromError(CommandError.ObjectNotFound, $"Could not find a game by that ID.")
                    : TypeReaderResult.FromSuccess(game);
            }
        }
    }
}
