using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Discord.Commands;
using MechHisui.FateGOLib.Modules;

namespace MechHisui.FateGOLib
{
    public static class FgoMetaModule
    {
        private static readonly FgoProfileConverter profileConverter = new FgoProfileConverter();
        public async static Task InitFgoModules(this CommandService commands, StatService statService)
        {
            await commands.AddModule<UpdateModule>();
            statService.RegisterUpdateFunc("all", () =>
            {
                return Task.WhenAll(statService.UpdateFuncs.Where(kv => kv.Key != "all")
                    .Select(kv => kv.Value()).ToList());
            });

            await commands.AddModule<ServantStatsModule>();
            statService.RegisterUpdateFunc("profiles", async () =>
            {
                Console.WriteLine("Updating profile lists...");
                FgoHelpers.ServantProfiles = JsonConvert.DeserializeObject<List<ServantProfile>>(await statService.ApiService.GetDataFromServiceAsJsonAsync("Servants"), profileConverter);
                FgoHelpers.FakeServantProfiles = JsonConvert.DeserializeObject<List<ServantProfile>>(await statService.ApiService.GetDataFromServiceAsJsonAsync("FakeServants"), profileConverter);
            });

            await commands.AddModule<CeStatsModule>();
            statService.RegisterUpdateFunc("ces", async () =>
            {
                Console.WriteLine("Updating CE list...");
                FgoHelpers.CEProfiles = JsonConvert.DeserializeObject<List<CEProfile>>(await statService.ApiService.GetDataFromServiceAsJsonAsync("CEs"));
            });

            await commands.AddModule<EventModule>();
            statService.RegisterUpdateFunc("events", async () =>
            {
                Console.WriteLine("Updating Event List...");
                FgoHelpers.EventList = JsonConvert.DeserializeObject<List<Event>>(await statService.ApiService.GetDataFromServiceAsJsonAsync("Events"));
            });

            await commands.AddModule<MysticCodeStatsModule>();
            statService.RegisterUpdateFunc("mystic", async () =>
            {
                Console.WriteLine("Updating Mystic Codes list...");
                FgoHelpers.MysticCodeList = JsonConvert.DeserializeObject<List<MysticCode>>(await statService.ApiService.GetDataFromServiceAsJsonAsync("MysticCodes"));
            });


            await commands.AddModule<GachaModule>();
            await commands.AddModule<HgwModule>();

            await statService.UpdateFuncs["all"]();
        }

        //protected readonly StatService StatService;


        //protected FgoMetaModule(StatService stats, CommandService commands)
        //{
        //    StatService = stats;

        //    Using.GetAwaiter().GetResult() here since there is no proper async context that await works
        //    StatService.UpdateProfileListsAsync().GetAwaiter().GetResult();
        //    StatService.UpdateCEListAsync().GetAwaiter().GetResult();
        //    StatService.UpdateEventListAsync().GetAwaiter().GetResult();
        //    StatService.UpdateMysticCodesListAsync().GetAwaiter().GetResult();
        //    StatService.UpdateDropsListAsync().GetAwaiter().GetResult();


        //    _modules.Add(new AliasModule(_statService, config));
        //    _modules.Add(new CeStatsModule(_statService, config));
        //    _modules.Add(new DropsModule(_statService, config));
        //    _modules.Add(new EventModule(_statService, config));
        //    _modules.Add(new GachaModule(_statService, config));
        //    _modules.Add(new HgwModule(_statService, config));
        //    _modules.Add(new MysticCodeStatsModule(_statService, config));
        //    _modules.Add(new ServantStatsModule(_statService, config));
        //    _modules.Add(new SimulateModule(_statService, config));

        //    _config = config;
        //}

        //        public void InstallModules(DiscordClient client)
        //        {
        //            foreach (var mod in _modules)
        //            {
        //                client.AddModule(mod);
        //            }

        //            Console.WriteLine("Registering 'Update'...");
        //            client.GetService<CommandService>().CreateCommand("update")
        //                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(_config["Owner"]))
        //                .Parameter("item", ParameterType.Optional)
        //                .Hide()
        //                .Do(async cea =>
        //                {
        //                    await cea.Channel.SendIsTyping();
        //                    switch (cea.Args[0])
        //                    {
        //                        case "alias":
        //                            _statService.ReadAliasList();
        //                            await cea.Channel.SendMessage("Updated alias lookups.");
        //                            break;
        //                        case "profiles":

        //                            break;
        //                        case "ces":
        //                            await _statService.UpdateCEListAsync();
        //                            await cea.Channel.SendMessage("Updated CE lookup.");
        //                            break;
        //                        case "events":
        //                            await _statService.UpdateEventListAsync();
        //                            await cea.Channel.SendMessage("Updated events lookup.");
        //                            break;
        //                        //case "fcs":
        //                        //    FriendCodes.ReadFriendData(config["FriendcodePath"]);
        //                        //    await cea.Channel.SendMessage("Updated friendcodes");
        //                        //    break;
        //                        case "mystic":
        //                            await _statService.UpdateMysticCodesListAsync();
        //                            await cea.Channel.SendMessage("Updated Mystic Codes lookup.");
        //                            break;
        //                        case "drops":
        //                            await _statService.UpdateDropsListAsync();
        //                            await cea.Channel.SendMessage("Updated Item Drops lookup.");
        //                            break;
        //                        default:
        //                            _statService.ReadAliasList();
        //                            //FriendCodes.ReadFriendData(config["FriendcodePath"]);
        //                            await _statService.UpdateProfileListsAsync();
        //                            await _statService.UpdateCEListAsync();
        //                            await _statService.UpdateEventListAsync();
        //                            await _statService.UpdateMysticCodesListAsync();
        //                            await _statService.UpdateDropsListAsync();
        //                            await cea.Channel.SendMessage("Updated all lookups.");
        //                            break;
        //                    }
        //                });
        //        }
    }
}
