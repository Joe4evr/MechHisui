using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using JiiLib.Net;
using Discord;
using Discord.Commands;
using Discord.Modules;

namespace MechHisui.FateGOLib.Modules
{
    public class FgoStatsMetaModule
    {
        private readonly StatService _statService;
        private readonly IConfiguration _config;
        private readonly List<IModule> _modules = new List<IModule>();

        public FgoStatsMetaModule(IConfiguration config)
        {
            Console.WriteLine("Connecting to data service (FGO)...");
            var apiService = new GoogleScriptApiService(
                Path.Combine(config["Google_Fgo_Profiles"], "client_secret.json"),
                Path.Combine(config["Google_Fgo_Profiles"], "scriptcreds"),
                "MechHisui",
                config["Fgo_Key"],
                "exportSheet",
                new string[]
                {
                    "https://www.googleapis.com/auth/spreadsheets",
                    "https://www.googleapis.com/auth/drive",
                    "https://spreadsheets.google.com/feeds/"
                });

            _statService = new StatService(apiService,
                servantAliasPath: Path.Combine(config["AliasPath"], "servants.json"),
                ceAliasPath: Path.Combine(config["AliasPath"], "ces.json"),
                mysticAliasPath: Path.Combine(config["AliasPath"], "mystics.json"));

            try
            {
                //Using .GetAwaiter().GetResult() here since there is no proper async context that await works
                _statService.UpdateProfileListsAsync().GetAwaiter().GetResult();
                _statService.UpdateCEListAsync().GetAwaiter().GetResult();
                _statService.UpdateEventListAsync().GetAwaiter().GetResult();
                _statService.UpdateMysticCodesListAsync().GetAwaiter().GetResult();
                _statService.UpdateDropsListAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now}: {ex.Message}");
                Environment.Exit(0);
            }

            _modules.Add(new AliasModule(_statService, config));
            _modules.Add(new CeStatsModule(_statService, config));
            _modules.Add(new DropsModule(_statService, config));
            _modules.Add(new EventModule(_statService, config));
            _modules.Add(new GachaModule(_statService, config));
            _modules.Add(new HgwModule(_statService, config));
            _modules.Add(new MysticCodeStatsModule(_statService, config));
            _modules.Add(new ServantStatsModule(_statService, config));
            _modules.Add(new SimulateModule(_statService, config));

            _config = config;
        }

        public void InstallModules(DiscordClient client)
        {
            foreach (var mod in _modules)
            {
                client.AddModule(mod);
            }

            Console.WriteLine("Registering 'Update'...");
            client.GetService<CommandService>().CreateCommand("update")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(_config["Owner"]))
                .Parameter("item", ParameterType.Optional)
                .Hide()
                .Do(async cea =>
                {
                    await cea.Channel.SendIsTyping();
                    switch (cea.Args[0])
                    {
                        case "alias":
                            _statService.ReadAliasList();
                            await cea.Channel.SendWithRetry("Updated alias lookups.");
                            break;
                        case "profiles":
                            await _statService.UpdateProfileListsAsync();
                            await cea.Channel.SendWithRetry("Updated profile lookups.");
                            break;
                        case "ces":
                            await _statService.UpdateCEListAsync();
                            await cea.Channel.SendWithRetry("Updated CE lookup.");
                            break;
                        case "events":
                            await _statService.UpdateEventListAsync();
                            await cea.Channel.SendWithRetry("Updated events lookup.");
                            break;
                        //case "fcs":
                        //    FriendCodes.ReadFriendData(config["FriendcodePath"]);
                        //    await cea.Channel.SendWithRetry("Updated friendcodes");
                        //    break;
                        case "mystic":
                            await _statService.UpdateMysticCodesListAsync();
                            await cea.Channel.SendWithRetry("Updated Mystic Codes lookup.");
                            break;
                        case "drops":
                            await _statService.UpdateDropsListAsync();
                            await cea.Channel.SendWithRetry("Updated Item Drops lookup.");
                            break;
                        default:
                            _statService.ReadAliasList();
                            //FriendCodes.ReadFriendData(config["FriendcodePath"]);
                            await _statService.UpdateProfileListsAsync();
                            await _statService.UpdateCEListAsync();
                            await _statService.UpdateEventListAsync();
                            await _statService.UpdateMysticCodesListAsync();
                            await _statService.UpdateDropsListAsync();
                            await cea.Channel.SendWithRetry("Updated all lookups.");
                            break;
                    }
                });
        }
    }
}
