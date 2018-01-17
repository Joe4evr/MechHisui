using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Addons.MpGame;
using Newtonsoft.Json;
using MechHisui.Superfight.Models;

namespace MechHisui.Superfight
{
    public sealed class SuperfightService : MpGameService<SuperfightGame, SuperfightPlayer>
    {
        public SuperfightConfig Config { get; }
        internal ConcurrentDictionary<ulong, int> DiscussionTimer { get; } = new ConcurrentDictionary<ulong, int>();

        public SuperfightService(
            DiscordSocketClient client,
            string cfgBasepath,
            Func<LogMessage, Task> logger = null)
            : base(client, logger)
        {
            if (cfgBasepath == null)
                throw new ArgumentNullException(nameof(cfgBasepath));

            Config = new SuperfightConfig(
                JsonConvert.DeserializeObject<IEnumerable<string>>(File.ReadAllText(Path.Combine(cfgBasepath, "sf_chara.json"))),
                JsonConvert.DeserializeObject<IEnumerable<string>>(File.ReadAllText(Path.Combine(cfgBasepath, "sf_ability.json"))),
                JsonConvert.DeserializeObject<IEnumerable<string>>(File.ReadAllText(Path.Combine(cfgBasepath, "sf_location.json"))));
        }
    }

    //public static class SFExtensions
    //{
    //    public static Task AddSuperFight(
    //        this CommandService cmds,
    //        IServiceCollection map,
    //        string cfgBasepath)
    //    {
    //        map.AddSingleton(new SuperfightService(cfgBasepath));
    //        return cmds.AddModuleAsync<SuperfightModule>();
    //    }
    //}
}
