using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace MechHisui.SymphoXDULib
{
    public sealed class XduStatService
    {
        internal IXduConfig Config { get; }

        private readonly ConcurrentDictionary<ulong, PaginatedMessage> _paginated = new ConcurrentDictionary<ulong, PaginatedMessage>();
        internal const string EmoteBack = "◀";
        internal const string EmoteNext = "▶";
        internal const string EmoteStop = "❌";
        internal const string EmoteSelect = "✅";
        private readonly Func<LogMessage, Task> _logger;
        private readonly BaseSocketClient _sockClient;

        public XduStatService(
            IXduConfig config,
            BaseSocketClient client,
            Func<LogMessage, Task>? logger = null)
        {
            Config = config;
            client.ReactionAdded += Client_ReactionAdded;
            //client.ReactionRemoved += Client_ReactionRemoved;
            //client.ReactionsCleared += Client_ReactionsCleared;
            client.MessageDeleted += Client_MessageDeleted;
            _sockClient = client;
            _logger = logger ?? (msg => Task.CompletedTask);
        }

        internal Task Log(LogSeverity severity, string msg)
            => _logger(new LogMessage(severity, "XDU", msg));

        internal Task AddNewPagedAsync(PaginatedMessage pm)
            => Task.FromResult(_paginated.TryAdd(pm.Msg!.Id, pm));

        private async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!message.HasValue)
            {
                await Log(LogSeverity.Debug, $"Message with id {message.Id} was not in cache.").ConfigureAwait(false);
                return;
            }
            if (!reaction.User.IsSpecified)
            {
                await Log(LogSeverity.Debug, $"Message with id {message.Id} had invalid user.").ConfigureAwait(false);
                return;
            }
            var msg = message.Value;
            if (_paginated.TryGetValue(msg.Id, out var pagedmsg))
            {
                if (reaction.UserId == _sockClient.CurrentUser.Id) return;

                if (reaction.UserId != pagedmsg.UserId)
                {
                    await msg.RemoveReactionAsync(reaction.Emote, reaction.User.Value).ConfigureAwait(false);
                    return;
                }

                switch (reaction.Emote.Name)
                {
                    case EmoteBack:
                        await pagedmsg.BackAsync().ConfigureAwait(false);
                        break;
                    case EmoteNext:
                        await pagedmsg.NextAsync().ConfigureAwait(false);
                        break;
                    case EmoteStop:
                        await pagedmsg.Delete().ConfigureAwait(false);
                        break;
                    case EmoteSelect:
                        if (pagedmsg.ListenForSelect)
                        {
                            var desc = pagedmsg.Msg!.Embeds.FirstOrDefault()?.Description;
                            if (desc != null && Int32.TryParse(GetId(desc), out var id))
                            {
                                await Task.WhenAll(
                                    pagedmsg.Msg.ModifyAsync(m => m.Embed = XduModule.XduCharacters.FormatCharacter(Config.GetGear(id))),
                                    pagedmsg.Msg.RemoveReactionAsync(reaction.Emote, reaction.User.Value),
                                    pagedmsg.Msg.RemoveReactionAsync(reaction.Emote, _sockClient.CurrentUser)).ConfigureAwait(false);
                                pagedmsg.ListenForSelect = false;
                            }
                        }
                        else
                        {
                            await pagedmsg.Msg!.RemoveReactionAsync(reaction.Emote, reaction.User.Value).ConfigureAwait(false);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        //private Task Client_ReactionRemoved(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        //{
        //    throw new NotImplementedException();
        //}

        //private Task Client_ReactionsCleared(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel)
        //{
        //    throw new NotImplementedException();
        //}

        private Task Client_MessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
            => Task.FromResult(_paginated.TryRemove(message.Id, out var _));

        private static string GetId(string input)
            => Regex.Match(input, @"#(?<id>[0-9]+):", RegexOptions.Singleline).Groups["id"].Value;
    }
}
