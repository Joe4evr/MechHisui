using System;
using Microsoft.EntityFrameworkCore;

namespace MechHisui.Core
{
    public partial class MechHisuiConfig
    {
        public DbSet<SecretHitlerTheme> SHThemes { get; set; }
    }
}
