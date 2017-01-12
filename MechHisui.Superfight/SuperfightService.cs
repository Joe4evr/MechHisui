using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Discord.Addons.MpGame;
using MechHisui.Superfight.Models;

namespace MechHisui.Superfight
{
    public sealed class SuperfightService : MpGameService<SuperfightGame, SuperfightPlayer>
    {
        public SuperfightConfig Config { get; }
        internal Dictionary<ulong, int> DiscussionTimer { get; } = new Dictionary<ulong, int>();

        public SuperfightService(string cfgBasepath)
        {
            Config = new SuperfightConfig(
                JsonConvert.DeserializeObject<IEnumerable<string>>(File.ReadAllText(Path.Combine(cfgBasepath, "sf_chara.json"))),
                JsonConvert.DeserializeObject<IEnumerable<string>>(File.ReadAllText(Path.Combine(cfgBasepath, "sf_ability.json"))),
                JsonConvert.DeserializeObject<IEnumerable<string>>(File.ReadAllText(Path.Combine(cfgBasepath, "sf_location.json"))));
        }
    }
}
