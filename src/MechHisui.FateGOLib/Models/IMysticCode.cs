using System.Collections.Generic;

namespace MechHisui.FateGOLib
{
    public interface IMysticCode
    {
        string Name { get; }
        string Skill1 { get; }
        string Skill1Effect { get; }
        string Skill2 { get; }
        string Skill2Effect { get; }
        string Skill3 { get; }
        string Skill3Effect { get; }
        string Image { get; }

        IEnumerable<IMysticAlias> Aliases { get; }
    }
}