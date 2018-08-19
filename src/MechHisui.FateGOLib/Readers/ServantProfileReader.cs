using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using SharedExtensions;

namespace MechHisui.FateGOLib
{
    public partial class FgoModule
    {
        private sealed class ServantProfileReader : TypeReader
        {
            private static readonly string[] _neverEver = new[] { "arc", "arcueid" };
            private readonly IFgoConfig _config;

            public ServantProfileReader(IFgoConfig config)
            {
                _config = config;
            }

            public override async Task<TypeReaderResult> ReadAsync(
                ICommandContext context,
                string input,
                IServiceProvider services)
            {
                if (Int32.TryParse(input, out var id))
                {
                    var res = await _config.GetServantAsync(id).ConfigureAwait(false);
                    return (res != null)
                        ? TypeReaderResult.FromSuccess(res)
                        : TypeReaderResult.FromError(CommandError.ObjectNotFound, "No servant known by that ID.");
                }

                if (input.ContainsIgnoreCase("waifu"))
                {
                    await context.Channel.SendMessageAsync("It has come to my attention that your 'waifu' is equatable to fecal matter.");
                    return TypeReaderResult.FromError(CommandError.ObjectNotFound, "");
                }

                if (_neverEver.ContainsIgnoreCase(input))
                {
                    await context.Channel.SendMessageAsync("Never ever.");
                    return TypeReaderResult.FromError(CommandError.ObjectNotFound, "");
                }

                var potentials = await _config.FindServantsAsync(input).ConfigureAwait(false);
                if (potentials.Count() == 1)
                {
                    return TypeReaderResult.FromSuccess(potentials.Single());
                }
                else if (potentials.Count() > 1)
                {
                    var sb = new StringBuilder("Entry ambiguous. Did you mean one of the following?\n")
                        .AppendSequence(potentials, (s, pr) => s.AppendLine($"**{pr.Name}** *({String.Join(", ", pr.Aliases)})*"));

                    await context.Channel.SendMessageAsync(sb.ToString());
                    return TypeReaderResult.FromError(CommandError.ObjectNotFound, "");
                }
                else
                {
                    await context.Channel.SendMessageAsync("No such entry found. Please try another name.");
                    return TypeReaderResult.FromError(CommandError.ObjectNotFound, "");
                }
            }
        }
    }
}
