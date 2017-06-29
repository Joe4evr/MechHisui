using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Addons.SimpleAudio;
using Discord.Addons.SimplePermissions;
using Microsoft.EntityFrameworkCore;

namespace Kohaku
{
    //internal class KohakuConfig : JsonConfigBase
    //{
    //    public string LoginToken { get; set; }

    //    public AudioConfig AudioConfig { get; set; }

    //    public Dictionary<string, string[]> TriviaData { get; set; }

    //    //[JsonIgnore]
    //    //public List<ServantProfile> TestProfiles { get; set; }
    //}

    internal class KohakuConfig : EFBaseConfigContext
    {


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.
            base.OnConfiguring(optionsBuilder);
        }
    }
}
