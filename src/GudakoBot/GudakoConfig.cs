using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace GudakoBot
{
    public sealed class GudakoConfig
    {
        public string LoginToken { get; set; }

        public ulong FgoGeneral { get; set; }

        public IEnumerable<string> Lines { get; set; }
    }

    internal sealed class ConfigStore
    {
        private readonly string _path;
        private readonly GudakoConfig _config;

        public ConfigStore(string path)
        {
            _path = path;
            _config = JsonConvert.DeserializeObject<GudakoConfig>(File.ReadAllText(path));
        }

        public GudakoConfig Load() => _config;
        //public void Save() => File.WriteAllText(_path, JsonConvert.SerializeObject(_config));
    }
}
