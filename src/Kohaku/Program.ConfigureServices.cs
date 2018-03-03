using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
//using System.Text.RegularExpressions;
using System.Threading.Tasks;
//using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
//using Discord.Addons.SimpleAudio;
using Discord.Addons.SimplePermissions;
using MechHisui.Core;
using SharedExtensions;

namespace Kohaku
{
    using MechHisuiConfigStore = EFConfigStore<MechHisuiConfig, HisuiGuild, HisuiChannel, HisuiUser>;
#pragma warning disable CA2007, CA1001
    internal partial class Program
    {
        private static readonly Emoji _litter = new Emoji("\uD83D\uDEAE");

        private static IServiceProvider ConfigureServices(
            DiscordSocketClient client,
            CommandService commands,
            Params p,
            //IConfigStore<KohakuConfig> store,
            Func<LogMessage, Task> logger = null)
        {
            logger?.Invoke(new LogMessage(LogSeverity.Verbose, "Main", "Constructing ConfigStore"));
            var store = new MechHisuiConfigStore(commands,
                options => options
                    //.UseSqlServer(p.ConnectionString)
                    .UseSqlite(p.ConnectionString)
                );


            var map = new ServiceCollection()
                //.AddSingleton(new TestService(store))
                .AddSingleton(new PermissionsService(store, commands, client, logger));

            //using (var config = store.Load())
            //{
            //    string ffmpeg = config.Strings.SingleOrDefault(t => t.Key == "FFMpegPath")?.Value;
            //    string musicpath = config.Strings.SingleOrDefault(t => t.Key == "MusicBasePath")?.Value;
            //    var audioCfg = new AudioConfig(
            //        ffmpegPath: ffmpeg,
            //        musicBasePath: musicpath)
            //    {
            //        GuildConfigs =
            //        {
            //            [161445678633975808] = new StandardAudioGuildConfig
            //            {
            //                VoiceChannelId = 161445679418441729,
            //                MessageChannelId = 161445678633975808,
            //                AutoConnect = true,
            //                AutoPlay = false,
            //                AllowCommands = true,
            //                AllowReactions = true,
            //            }
            //        }
            //    };

            //    map.AddSingleton(new AudioService(client, audioCfg, logger));

            //    //var fgoCfg = 
            //}


            return map.BuildServiceProvider();
        }

        //private async Task InitCommands()
        //{
        //    //await _commands.UseSimplePermissions(_client, _store, _map, _logger);

        //    await _commands.AddModuleAsync<TestModule>();

        //    //var eval = EvalService.Builder.BuilderWithSystemAndLinq()
        //    //    //.Add(new EvalReference(typeof(FgoStatService)))
        //    //    .Add(new EvalReference(typeof(ICommandContext)));

        //    //_map.AddSingleton(eval.Build(
        //    //    _logger,
        //    //    //(typeof(FgoStatService), "FgoStats"),
        //    //    (typeof(ICommandContext), "Context")
        //    //));
        //    //await _commands.AddModuleAsync<EvalModule>();

        //    //var fgo = new FgoConfig
        //    //{
        //    //    FindServants = term =>
        //    //    {
        //    //        using (var config = _store.Load())
        //    //        {
        //    //            return config.Servants.Where(s => s.Name.Equals(term, StringComparison.OrdinalIgnoreCase))
        //    //                .Concat(config.ServantAliases.Where(a => a.Alias.Equals(term, StringComparison.OrdinalIgnoreCase))
        //    //                    .Select(a => a.Servant))
        //    //                .Distinct();
        //    //        }
        //    //    },
        //    //    AddServantAlias = (name, alias) =>
        //    //    {
        //    //        using (var config = _store.Load())
        //    //        {
        //    //            if (config.Servants.Any(s => s.Aliases.Any(a => a.Alias == alias)))
        //    //            {
        //    //                return false;
        //    //            }
        //    //            else
        //    //            {
        //    //                var srv = config.Servants.SingleOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        //    //                if (srv != null)
        //    //                {
        //    //                    var al = new ServantAlias { Servant = srv, Alias = alias };
        //    //                    srv.Aliases.Add(al);
        //    //                    config.Save();
        //    //                    return true;
        //    //                }
        //    //                else
        //    //                {
        //    //                    return false;
        //    //                }
        //    //            }
        //    //        }

        //    //    },
        //    //};
        //    //await _commands.UseFgoService(_map, fgo, _client);

        //}

        private async Task CheckReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!message.HasValue)
            {
                await Log(LogSeverity.Debug, $"Message with id {message.Id} was not in cache.");
                return;
            }
            if (!reaction.User.IsSpecified)
            {
                await Log(LogSeverity.Debug, $"Message with id {message.Id} had an unspecified user.");
                return;
            }
            var msg = message.Value;

            if (msg.Author.Id == _client.CurrentUser.Id
                && reaction.User.Value.Id == (await _client.GetApplicationInfoAsync()).Owner.Id
                && reaction.Emote.Name == _litter.Name)
            {
                await msg.DeleteAsync();
                return;
            }
        }
    }
}
