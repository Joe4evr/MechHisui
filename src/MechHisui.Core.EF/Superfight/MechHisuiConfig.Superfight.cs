using System;
using Microsoft.EntityFrameworkCore;

namespace MechHisui.Core
{
    public partial class MechHisuiConfig
    {
        public DbSet<SuperfightCard> SFCards { get; set; }
    }
}
