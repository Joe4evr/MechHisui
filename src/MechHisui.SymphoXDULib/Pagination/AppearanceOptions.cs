using Discord;

namespace MechHisui.SymphoXDULib
{
    internal class AppearanceOptions
    {
        public IEmote EmoteFirst { get; set; }
        public IEmote EmoteLast { get; set; }
        public IEmote EmoteBack { get; set; }
        public IEmote EmoteNext { get; set; }
        public IEmote EmoteStop { get; set; }
    }
}