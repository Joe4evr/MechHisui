using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace MechHisui.SymphoXDULib
{
    internal class PaginatedMessage
    {
        private IReadOnlyList<Embed> _pages;
        private readonly IUser _user;
        private readonly AppearanceOptions _options;
        private readonly IEmote[] _emotes;

        public ulong UserId => _user.Id;
        private int _totalPages => _pages.Count;

        private int _currentPage = 0;
        internal IUserMessage? Msg { get; private set; }
        internal bool ListenForSelect { get;  set; }

        public PaginatedMessage(IReadOnlyList<Embed> pages, IUser user, AppearanceOptions options, bool listenForSelect = false)
        {
            _pages = pages;
            _user = user;
            _options = options;
            _emotes = new[] { options.EmoteBack!, options.EmoteNext!, options.EmoteStop! };
            ListenForSelect = listenForSelect;
        }

        public async Task<PaginatedMessage> SendMessage(IMessageChannel channel)
        {
            Msg = await channel.SendMessageAsync("", embed: _pages[0]).ConfigureAwait(false);
            await Msg.AddReactionsAsync(_emotes).ConfigureAwait(false);
            //await Msg.AddReactionAsync(_options.EmoteBack).ConfigureAwait(false);
            //await Msg.AddReactionAsync(_options.EmoteNext).ConfigureAwait(false);
            //await Msg.AddReactionAsync(_options.EmoteStop).ConfigureAwait(false);

            return this;
        }

        public async Task BackAsync()
        {
            await Msg!.RemoveReactionAsync(_options.EmoteBack, _user).ConfigureAwait(false);
            if (_currentPage == 0) return;

            await Msg.ModifyAsync(m => m.Embed = _pages[--_currentPage]).ConfigureAwait(false);
        }

        public async Task NextAsync()
        {
            await Msg!.RemoveReactionAsync(_options.EmoteNext, _user).ConfigureAwait(false);
            if (_currentPage == (_totalPages - 1)) return;

            await Msg.ModifyAsync(m => m.Embed = _pages[++_currentPage]).ConfigureAwait(false);
        }

        public Task Delete()
        {
            return Msg!.DeleteAsync();
        }

        public Task ResetPages(IReadOnlyList<Embed> pages)
        {
            _pages = pages;
            _currentPage = 0;
            return Msg!.ModifyAsync(m => m.Embed = _pages[_currentPage]);
        }

        //private Embed GetPage(int i)
        //{
        //    var page = _pages[i];
        //    var builder = new EmbedBuilder()
        //        .WithTitle(_title)
        //        .WithDescription(page.Description);
        //    if (page.ImageUrl != null)
        //    {
        //        builder.WithImageUrl(page.ImageUrl);
        //    }
        //    builder.WithFooter(footer => footer.WithText($"Page {i+1}/{_pages.Count}"));

        //    return builder.Build();
        //}
    }
}