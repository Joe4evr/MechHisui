using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Addons.MpGame;
using Discord.Commands;
using MechHisui.Superfight.Models;
using Newtonsoft.Json;

namespace MechHisui.Superfight
{
    public sealed class SuperfightService : MpGameService<SuperfightGame, SuperfightPlayer>
    {
        public SuperfightConfig Config { get; }
        internal Dictionary<ulong, int> DiscussionTimer { get; } = new Dictionary<ulong, int>();

        internal SuperfightService(string cfgBasepath)
        {
            Config = new SuperfightConfig(
                JsonConvert.DeserializeObject<IEnumerable<string>>(File.ReadAllText(Path.Combine(cfgBasepath, "sf_chara.json"))),
                JsonConvert.DeserializeObject<IEnumerable<string>>(File.ReadAllText(Path.Combine(cfgBasepath, "sf_ability.json"))),
                JsonConvert.DeserializeObject<IEnumerable<string>>(File.ReadAllText(Path.Combine(cfgBasepath, "sf_location.json"))));
        }
    }

    public static class SFExtensions
    {
        public static Task AddSuperFight(
            this CommandService cmds,
            IServiceCollection map,
            string cfgBasepath)
        {
            map.AddSingleton(new SuperfightService(cfgBasepath));
            return cmds.AddModuleAsync<SuperfightModule>();
        }
    }
}
