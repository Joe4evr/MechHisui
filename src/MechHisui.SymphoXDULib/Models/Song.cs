using System.Collections.Generic;

namespace MechHisui.SymphoXDULib
{
    public class Song
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Effect { get; set; }
        public string Image { get; set; }
        public ICollection<string> EquipsOn { get; set; }
    }
}