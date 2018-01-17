using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.SimplePermissions;
using Discord.Commands;


namespace MechHisui.SymphoXDULib
{
    public partial class XduModule
    {
        [Group("song"), Alias("songs"), Permission(MinimumPermission.Everyone)]
        public class XduSongs : ModuleBase<SocketCommandContext>
        {
            private readonly XduStatService _stats;

            public XduSongs(XduStatService stats)
            {
                _stats = stats;
            }

            [Command]
            public Task GetSong(int id)
            {
                var song = _stats.Config.GetSong(id);
                return (song != null)
                    ? ReplyAsync("", embed: FormatSong(song))
                    : ReplyAsync("Unknown/Not a Song ID");
            }   

            [Command("search"), Alias("for", "by")]
            public Task Find(string singer)
            {
                var songs = _stats.Config.AllSongs()
                    .Where(s => s.EquipsOn.Contains(singer, StringComparer.OrdinalIgnoreCase))
                    .OrderBy(s => s.Id)
                    .Select(s => FormatSong(s)).ToList();
                return SendResults(songs, _stats, Context, listenForSelect: false);
            }

            //private async Task SendResults(List<Embed> pages)
            //{
            //    if (pages.Count == 0)
            //    {
            //        await ReplyAsync("No results found");
            //        return;
            //    }

            //    var pmsg = new PaginatedMessage(pages,
            //        //embedColor: new Color(),
            //        user: Context.User,
            //        options: _options);

            //    await _stats.AddNewPagedAsync(await pmsg.SendMessage(Context.Channel));
            //}

            internal static Embed FormatSong(XduSong song)
            {
                return new EmbedBuilder
                {
                    Author      = new EmbedAuthorBuilder { Name = $"Song #{song.Id}: {song.Title}" },
                    Title       = "Effect:",
                    Description = song.Effect,
                    Fields      =
                    {
                        new EmbedFieldBuilder
                        {
                            IsInline = false,
                            Name     = "Usable by:",
                            Value    = String.Join(", ", song.EquipsOn)
                        }
                    },
                    ImageUrl    = song.Image ?? "http://i.imgur.com/hPNxdda.png"
                }.Build();
            }
        }
    }
}
