using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.SimplePermissions;
using Discord.Commands;
using Discord.Commands.Builders;
using JiiLib.SimpleDsl;
using NodaTime;
using SharedExtensions;

namespace MechHisui.FateGOLib
{
    [Name("FGO"), RequireContext(ContextType.Guild)]
    //[Permission(MinimumPermission.Everyone)]
    public partial class FgoModule : ModuleBase
    {
        private readonly FgoStatService _service;
        public FgoModule(FgoStatService service)
        {
            _service = service;
        }

        protected override void OnModuleBuilding(CommandService commandService, ModuleBuilder builder)
        {
            base.OnModuleBuilding(commandService, builder);

            if (!commandService.TypeReaders.Contains(typeof(ZonedDateTimeReader)))
                commandService.AddTypeReader<ZonedDateTime>(new ZonedDateTimeReader());

            if (!commandService.TypeReaders.Contains(typeof(ServantFilterTypeReader)))
                commandService.AddTypeReader<QueryParseResult<IServantProfile>>(new ServantFilterTypeReader());

            if (!commandService.TypeReaders.Contains(typeof(ServantProfileReader)))
                commandService.AddTypeReader<IServantProfile>(new ServantProfileReader(_service.Config));
        }
    }
}
