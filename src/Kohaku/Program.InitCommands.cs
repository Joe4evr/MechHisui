using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
//using Discord.Addons.SimpleAudio;
using Discord.Addons.SimplePermissions;
//using MechHisui.FateGOLib;
using Newtonsoft.Json;

namespace Kohaku
{
    internal partial class Program
    {
        private IServiceProvider _services;

        private async Task InitCommands()
        {
            await _commands.UseSimplePermissions(_client, _store, _map, _logger);

            var eval = EvalService.Builder.BuilderWithSystemAndLinq()
                //.Add(new EvalReference(typeof(FgoStatService)))
                .Add(new EvalReference(typeof(ICommandContext)));

            _map.AddSingleton(eval.Build(
                _logger,
                //(typeof(FgoStatService), "FgoStats"),
                (typeof(ICommandContext), "Context")
            ));
            await _commands.AddModuleAsync<EvalModule>();

            //var fgo = new FgoConfig
            //{
            //    GetServants = () => JsonConvert.DeserializeObject<List<ServantProfile>>(File.ReadAllText("Servants.json")),
            //    //GetFakeServants = Enumerable.Empty<ServantProfile>,
            //    //GetServantAliases = () =>
            //    //{
            //    //    using (var config = _store.Load())
            //    //    {
            //    //        return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(config.FgoBasePath, "ServantAlias.json")))
            //    //            .Join(config.GetAllServants(), kv => kv.Value, s => s.Name, (kv, s) => new ServantAlias { Alias = kv.Key, Servant = s });
            //    //    }
            //    //},
            //    AddServantAlias = (name, alias) => false,
            //    GetCEs = Enumerable.Empty<CEProfile>,
            //    //GetCEAliases = () =>
            //    //{
            //    //    using (var config = _store.Load())
            //    //    {
            //    //        return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(config.FgoBasePath, "CEAlias.json")))
            //    //            .Join(config.GetAllCEs(), kv => kv.Value, ce => ce.Name, (kv, ce) => new CEAlias { Alias = kv.Key, CE = ce });
            //    //    }
            //    //},
            //    AddCEAlias = (ce, alias) => false,
            //    GetMystics = Enumerable.Empty<MysticCode>,
            //    //GetMysticAliases = () =>
            //    //{
            //    //    using (var config = _store.Load())
            //    //    {
            //    //        return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(config.FgoBasePath, "MysticAlias.json")))
            //    //            .Join(config.GetAllMystics(), kv => kv.Value, myst => myst.Code, (kv, myst) => new MysticAlias { Alias = kv.Key, Code = myst });
            //    //    }
            //    //},
            //    AddMysticAlias = (code, alias) => false,
            //    GetEvents = Enumerable.Empty<FgoEvent>
            //};
            //await _commands.UseFgoService(_map, fgo, _client);

            //using (var config = _store.Load())
            //{
            //    //await _commands.AddTrivia<TriviaImpl>(_client, _map, config.TriviaData, _logger);

            //    //await _commands.UseAudio<AudioModuleImpl>(_map, config.AudioConfig, _logger);
            //}

            _client.MessageReceived += HandleCommand;
            _services = _map.BuildServiceProvider();
        }
    }
}
