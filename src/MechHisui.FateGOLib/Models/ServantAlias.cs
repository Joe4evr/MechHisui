using System;

namespace MechHisui.FateGOLib
{
    public class ServantAlias
    {
        public int Id { get; set; }
        public string Alias { get; set; }
        public ServantProfile Servant { get; set; }
    }
}
