﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Addons.SimplePermissions;
using MechHisui.FateGOLib;
using MechHisui.HisuiBets;
using MechHisui.SecretHitler;
using MechHisui.Superfight;
using MechHisui.SymphoXDULib;
using Newtonsoft.Json;

namespace MechHisui
{
    public partial class Program
    {
        private async Task InitCommands()
        {
            var fgo = new FgoConfig
            {
                FindServants = term =>
                {
                    //    using (var config = _store.Load())
                    //    {
                    //        return config.Servants.Where(s => s.Name.Equals(term, StringComparison.OrdinalIgnoreCase))
                    //            .Concat(config.ServantAliases.Where(a => a.Alias.Equals(term, StringComparison.OrdinalIgnoreCase))
                    //                .Select(a => a.Servant))
                    //            .Distinct();
                    //    }
                    using (var config = _store.Load())
                    {
                        return config.GetAllServants()
                            .Where(s => s.Name.Equals(term, StringComparison.OrdinalIgnoreCase) || s.Aliases.Any(a => a.Alias.Equals(term, StringComparison.OrdinalIgnoreCase)))
                            .Distinct();
                    }
                },
                //AddServantAlias = (name, alias) =>
                //{
                //    using (var config = _store.Load())
                //    {
                //        if (config.Servants.Any(s => s.Aliases.Any(a => a.Alias == alias)))
                //        {
                //            return false;
                //        }
                //        else
                //        {
                //            var srv = config.Servants.SingleOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                //            if (srv != null)
                //            {
                //                var al = new ServantAlias { Servant = srv, Alias = alias };
                //                srv.Aliases.Add(al);
                //                config.Save();
                //                return true;
                //            }
                //            else
                //            {
                //                return false;
                //            }
                //        }
                //    }

                //},
                //GetFakeServants = () =>
                //{
                //    using (var config = _store.Load())
                //    {
                //        return JsonConvert.DeserializeObject<List<ServantProfile>>(File.ReadAllText(Path.Combine(config.FgoBasePath, "FakeServants.json")));
                //    }
                //},
                //GetServantAliases = () =>
                //{
                //    using (var config = _store.Load())
                //    {
                //        return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(config.FgoBasePath, "ServantAlias.json")))
                //            .Join(config.GetAllServants(), kv => kv.Value, s => s.Name, (kv, s) => new ServantAlias { Alias = kv.Key, Servant = s });
                //    }
                //},

                GetCEs = () =>
                {
                    using (var config = _store.Load())
                    {
                        return config.GetAllCEs();
                    }
                },
                GetCEAliases = () =>
                {
                    using (var config = _store.Load())
                    {
                        return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(config.FgoBasePath, "CEAlias.json")))
                            .Join(config.GetAllCEs(), kv => kv.Value, ce => ce.Name, (kv, ce) => new CEAlias { Alias = kv.Key, CE = ce });
                    }
                },
                AddCEAlias = (ce, alias) =>
                {
                    using (var config = _store.Load())
                    {
                        var all = config.GetAllCEs();
                        var p = all.SingleOrDefault(c => c.Name == ce);
                        if (p == null)
                        {
                            return false;
                        }
                        else
                        {
                            p.Aliases.Add(alias);
                            File.WriteAllText(Path.Combine(config.FgoBasePath, "CEs.json"), JsonConvert.SerializeObject(all, Formatting.Indented));
                            return true;
                        }
                    }
                },
                GetMystics = () =>
                {
                    using (var config = _store.Load())
                    {
                        return config.GetAllMystics();
                    }
                },
                GetMysticAliases = () =>
                {
                    using (var config = _store.Load())
                    {
                        return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(config.FgoBasePath, "MysticAlias.json")))
                            .Join(config.GetAllMystics(), kv => kv.Value, myst => myst.Code, (kv, myst) => new MysticAlias { Alias = kv.Key, Code = myst });
                    }
                },
                AddMysticAlias = (code, alias) =>
                {
                    using (var config = _store.Load())
                    {
                        var all = config.GetAllMystics();
                        var mystic = all.SingleOrDefault(m => m.Code == code);
                        if (mystic == null)
                        {
                            return false;
                        }
                        else
                        {
                            mystic.Aliases.Add(alias);
                            File.WriteAllText(Path.Combine(config.FgoBasePath, "Mystics.json"), JsonConvert.SerializeObject(all, Formatting.Indented));
                            return true;
                        }
                    }
                },
                GetEvents = () =>
                {
                    using (var config = _store.Load())
                    {
                        return JsonConvert.DeserializeObject<List<FgoEvent>>(File.ReadAllText(Path.Combine(config.FgoBasePath, "Events.json")));
                    }
                }
            };
            //_map.AddSingleton(fgo);
            await _commands.UseFgoService(_map, fgo, _client);

            //var bank = new BankOfHisui
            //{
                
            //};
            //await _commands.UseHisuiBank(_map, bank, _client, _logger);

            //var xdu = new XduConfig
            //{
            //    GetGears = () =>
            //    {
            //        using (var config = _store.Load())
            //        {
            //            string path = config.XduBasePath;
            //            var hibiki = JsonConvert.DeserializeObject<List<Profile>>(File.ReadAllText(Path.Combine(path, "Hibiki.json")));
            //            hibiki.ForEach(p => p.CharacterName = "Hibiki Tachibana");
            //            var tsubasa = JsonConvert.DeserializeObject<List<Profile>>(File.ReadAllText(Path.Combine(path, "Tsubasa.json")));
            //            tsubasa.ForEach(p => p.CharacterName = "Tsubasa Kazanari");
            //            var chris = JsonConvert.DeserializeObject<List<Profile>>(File.ReadAllText(Path.Combine(path, "Chris.json")));
            //            chris.ForEach(p => p.CharacterName = "Chris Yukine");

            //            var maria = JsonConvert.DeserializeObject<List<Profile>>(File.ReadAllText(Path.Combine(path, "Maria.json")));
            //            maria.ForEach(p => p.CharacterName = "Maria Cadenzavna Eve");
            //            var shirabe = JsonConvert.DeserializeObject<List<Profile>>(File.ReadAllText(Path.Combine(path, "Shirabe.json")));
            //            shirabe.ForEach(p => p.CharacterName = "Shirabe Tsukuyomi");
            //            var kirika = JsonConvert.DeserializeObject<List<Profile>>(File.ReadAllText(Path.Combine(path, "Kirika.json")));
            //            kirika.ForEach(p => p.CharacterName = "Kirika Akatsuki");

            //            var serena = JsonConvert.DeserializeObject<List<Profile>>(File.ReadAllText(Path.Combine(path, "Serena.json")));
            //            serena.ForEach(p => p.CharacterName = "Serena Cadenzavna Eve");
            //            var kanade = JsonConvert.DeserializeObject<List<Profile>>(File.ReadAllText(Path.Combine(path, "Kanade.json")));
            //            kanade.ForEach(p => p.CharacterName = "Kanade Amou");
            //            var miku = JsonConvert.DeserializeObject<List<Profile>>(File.ReadAllText(Path.Combine(path, "Miku.json")));
            //            miku.ForEach(p => p.CharacterName = "Miku Kohinata");
            //            return hibiki.Concat(tsubasa).Concat(chris)
            //                .Concat(maria).Concat(shirabe).Concat(kirika)
            //                .Concat(serena).Concat(kanade).Concat(miku).ToList();
            //        }
            //    },
            //    GetMemorias = () =>
            //    {
            //        using (var config = _store.Load())
            //        {
            //            return JsonConvert.DeserializeObject<List<Memoria>>(File.ReadAllText(Path.Combine(config.XduBasePath, "Memoria.json")));
            //        }
            //    },
            //    GetSongs = () =>
            //    {
            //        using (var config = _store.Load())
            //        {
            //            return JsonConvert.DeserializeObject<List<Song>>(File.ReadAllText(Path.Combine(config.XduBasePath, "Songs.json")));
            //        }
            //    },
            //    GetEvents = () =>
            //    {
            //        using (var config = _store.Load())
            //        {
            //            return JsonConvert.DeserializeObject<List<XduEvent>>(File.ReadAllText(Path.Combine(config.XduBasePath, "Events.json")));
            //        }
            //    }
            //};
            //_map.AddSingleton(new XduStatService(xdu, _client));
            //await _commands.AddModuleAsync<XduModule>();
            
            //var eval = EvalService.Builder.BuilderWithSystemAndLinq()
            //    .Add(new EvalReference(MetadataReference.CreateFromFile(typeof(StatService).Assembly.Location),
            //        "MechHisui.FateGOLib"))
            //    .Build(@"(FgoConfig stats)
            //{
            //    Servants = () => stats.GetServants().Concat(stats.GetFakedServants());
            //    CEs = stats.GetCEs;
            //}
            //private readonly Func<IEnumerable<ServantProfile>> Servants;
            //private readonly Func<IEnumerable<CEProfile>> CEs;");
            //_map.AddSingleton(eval);
            //await _commands.AddModuleAsync<EvalModule>();

            await _commands.UseSimplePermissions(_client, _store, _map, _logger);
            await _commands.AddDiceRoll();

            using (var config = _store.Load())
            {
                //await _commands.AddSecretHitler(_map, JsonConvert.DeserializeObject<List<SecretHitlerConfig>>(File.ReadAllText(Path.Combine(config.SHConfigPath, "shitler.json"))));
                //await _commands.AddSuperFight(_map, config.SuperfightBasePath);
            }

            _client.MessageReceived += HandleCommand;
        }
    }
}
