using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using JiiLib.Net;

namespace MechHisui.PkmnGoLib
{
    public class PgoDataService
    {
        private readonly IJsonApiService _apiService;

        public PgoDataService(IJsonApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task Init()
        {
            PgoHelpers.KnownMons = JsonConvert.DeserializeObject<List<Pokemon>>(await _apiService.GetDataFromServiceAsJsonAsync("Mons"));
            PgoHelpers.StardustPerLevel = JsonConvert.DeserializeObject<List<StardustLevel>>(await _apiService.GetDataFromServiceAsJsonAsync("Stardust"));
            PgoHelpers.CPMultiplier = JsonConvert.DeserializeObject<List<CP>>(await _apiService.GetDataFromServiceAsJsonAsync("CP"));
        }
    }
}
