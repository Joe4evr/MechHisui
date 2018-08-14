using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.SimplePermissions;
using Discord.Commands;
using Discord.Commands.Builders;
//using NodaTime;
using SharedExtensions;

namespace MechHisui.FateGOLib
{
    [Name("FGO"), RequireContext(ContextType.Guild)]
    [Permission(MinimumPermission.Everyone)]
    public sealed partial class FgoModule : ModuleBase
    {
        protected override void OnModuleBuilding(CommandService commandService, ModuleBuilder builder)
        {
            base.OnModuleBuilding(commandService, builder);

            //if (!commandService.TypeReaders.Contains(typeof(LocalDateTimeReader)))
            //    commandService.AddTypeReader<LocalDateTime>(new LocalDateTimeReader());

            if (!commandService.TypeReaders.Contains(typeof(ServantFilterTypeReader)))
                commandService.AddTypeReader<ServantFilterOptions>(new ServantFilterTypeReader());
        }
    }
}
