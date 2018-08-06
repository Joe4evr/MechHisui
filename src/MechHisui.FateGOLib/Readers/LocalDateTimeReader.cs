using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace MechHisui.FateGOLib
{
    public partial class FgoModule : ModuleBase
    {
        private sealed class LocalDateTimeReader : TypeReader
        {
            public override Task<TypeReaderResult> ReadAsync(
                ICommandContext context,
                string input,
                IServiceProvider services)
            {
                throw new NotImplementedException();
            }
        }
    }
}
