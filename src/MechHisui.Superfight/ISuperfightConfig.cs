using System;
using System.Collections.Generic;

namespace MechHisui.Superfight
{
    public interface ISuperfightConfig
    {
        IEnumerable<string> GetCharacters();
        IEnumerable<string> GetAbilities();
        IEnumerable<string> GetLocations();
    }
}
