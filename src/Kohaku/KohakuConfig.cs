using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Addons.SimpleAudio;
using Discord.Addons.SimplePermissions;
using Newtonsoft.Json;

namespace Kohaku
{
    internal class KohakuConfig : JsonConfigBase
    {
        public string LoginToken { get; set; }

        public AudioConfig AudioConfig { get; set; }

        public Dictionary<string, string[]> TriviaData { get; set; }

        //public KohakuConfig()
        //{
        //    TestProfiles = JsonConvert.DeserializeObject<List<ServantProfile>>(
        //        File.ReadAllText("profiles.json"),
        //        new FgoProfileConverter());
        //}

        //[JsonIgnore]
        //public List<ServantProfile> TestProfiles { get; set; }
    }
}
