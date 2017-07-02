using System;
using System.Threading.Tasks;
using Discord.Addons.SimplePermissions;
using Discord.Commands;

namespace DivaBot
{
    [Name("Tags")]
    public class TagModule : ModuleBase<ICommandContext>
    {
        private readonly TagService _service;

        public TagModule(TagService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [Command("tag"), Summary("List all available tags.")]
        [Permission(MinimumPermission.Everyone)]
        public Task GetTag()
        {
            return ReplyAsync($"Available tags: `{String.Join("`, `", _service.GetTags())}`");
        }

        [Command("tag"), Summary("Display the value associated with a given tag.")]
        [Permission(MinimumPermission.Everyone)]
        public async Task GetTag([Remainder] string tag)
        {
            if (_service.TryGetResponse(tag, out var resp))
                await ReplyAsync($"{tag}: {resp}").ConfigureAwait(false);
        }

        [Command("addtag"), Summary("Add a tag or replace an existing tag with a value.")]
        [Permission(MinimumPermission.ModRole)]
        public Task AddTag(string tag, [Remainder] string response)
        {
            _service.SetTagAndResponse(tag, response);
            return ReplyAsync(":ok:");
        }
    }
}
