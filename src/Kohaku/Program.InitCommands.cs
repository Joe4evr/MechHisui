using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
//using Discord.Addons.SimpleAudio;
using Discord.Addons.SimplePermissions;
using MechHisui.FateGOLib;
using System.Text.RegularExpressions;
//using Newtonsoft.Json;

namespace Kohaku
{
    internal partial class Program
    {
        private IServiceProvider _services;

        private async Task InitCommands()
        {
            await _commands.UseSimplePermissions(_client, _store, _map, _logger);

            //var eval = EvalService.Builder.BuilderWithSystemAndLinq()
            //    //.Add(new EvalReference(typeof(FgoStatService)))
            //    .Add(new EvalReference(typeof(ICommandContext)));

            //_map.AddSingleton(eval.Build(
            //    _logger,
            //    //(typeof(FgoStatService), "FgoStats"),
            //    (typeof(ICommandContext), "Context")
            //));
            //await _commands.AddModuleAsync<EvalModule>();

            var fgo = new FgoConfig
            {
                FindServants = term =>
                {
                    using (var config = _store.Load())
                    {
                        return config.Servants.Where(s => s.Name.Equals(term, StringComparison.OrdinalIgnoreCase))
                            .Concat(config.ServantAliases.Where(a => a.Alias.Equals(term, StringComparison.OrdinalIgnoreCase))
                                .Select(a => a.Servant))
                            .Distinct();
                    }
                },
                AddServantAlias = (name, alias) =>
                {
                    using (var config = _store.Load())
                    {
                        if (config.Servants.Any(s => s.Aliases.Any(a => a.Alias == alias)))
                        {
                            return false;
                        }
                        else
                        {
                            var srv = config.Servants.SingleOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                            if (srv != null)
                            {
                                var al = new ServantAlias { Servant = srv, Alias = alias };
                                srv.Aliases.Add(al);
                                config.Save();
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }

                },
            };
            //await _commands.UseFgoService(_map, fgo, _client);

            //using (var config = _store.Load())
            //{
            //    await _commands.UseAudio<AudioModuleImpl>(_map,
            //        new AudioConfig
            //        {
            //            FFMpegPath = config.Strings.Single(s => s.Key == "FFMpegPath").Value,
            //            MusicBasePath = config.Strings.Single(s => s.Key == "MusicBasePath").Value,
            //        }, _logger);
            //}

            _client.MessageReceived += HandleCommand;
            _services = _map.BuildServiceProvider();
        }
    }
}
