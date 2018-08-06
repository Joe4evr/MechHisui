using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MechHisui.Superfight
{
    public interface ISuperfightConfig
    {
        IEnumerable<ISuperfightCard> GetAllCards();

        Task<IEnumerable<ISuperfightCard>> GetCharactersAsync();
        Task<IEnumerable<ISuperfightCard>> GetAbilitiesAsync();
        Task<IEnumerable<ISuperfightCard>> GetLocationsAsync();
    }
}
