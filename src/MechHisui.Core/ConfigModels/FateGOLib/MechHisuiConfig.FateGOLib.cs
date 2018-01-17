using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MechHisui.FateGOLib;

namespace MechHisui.Core
{
    public partial class MechHisuiConfig
    {
        //FGO
        public DbSet<ServantProfile> Servants { get; set; }
        public DbSet<ServantSkill> FgoSkills { get; set; }
        public DbSet<ServantTrait> Traits { get; set; }
        public DbSet<ServantAlias> ServantAliases { get; set; }

        public DbSet<CEProfile> CEs { get; set; }
        public DbSet<CEAlias> CEAliases { get; set; }

        public DbSet<MysticCode> MysticCodes { get; set; }
        public DbSet<MysticAlias> MysticAliases { get; set; }

        public DbSet<FgoEvent> FgoEvents { get; set; }

        public DbSet<NameOnlyServant> NameOnlyServants { get; set; }

        public DbSet<FgoFriendData> FgoFriendData { get; set; }
    }
}
