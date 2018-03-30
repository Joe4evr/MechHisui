using System;
using System.Collections.Generic;

namespace MechHisui.Superfight
{
    public interface ISuperfightConfig
    {
        IEnumerable<ISuperfightCard> GetAllCards();

        IEnumerable<ISuperfightCard> GetCharacters();
        IEnumerable<ISuperfightCard> GetAbilities();
        IEnumerable<ISuperfightCard> GetLocations();
    }
}
