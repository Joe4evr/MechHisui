using System.Collections.Generic;

namespace MechHisui.SymphoXDULib
{
    public interface IXduSong
    {
        int Id { get; }
        string Title { get; }
        string Effect { get; }
        string Image { get; }
        ICollection<string> EquipsOn { get; }
    }
}