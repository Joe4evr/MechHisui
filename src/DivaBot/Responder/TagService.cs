//using System;
//using System.Collections.Generic;
//using Discord.Addons.SimplePermissions;

//namespace DivaBot
//{
//    public class TagService
//    {
//        private readonly object _lock = new object();
//        private readonly IConfigStore<DivaBotConfig> _configstore;

//        internal TagService(IConfigStore<DivaBotConfig> configStore)
//        {
//            _configstore = configStore ?? throw new ArgumentNullException(nameof(configStore));
//            //_tagResponses = new ConcurrentDictionary<string, string>((configStore.Load().TagResponses ?? Enumerable.Empty<KeyValuePair<string, string>>()));
//        }

//        public bool TryGetResponse(string tag, out string response)
//        {
//            using (var config = _configstore.Load())
//            {
//                return config.TagResponses.TryGetValue(tag, out response);
//            }
//        }

//        public IEnumerable<string> GetTags()
//        {
//            using (var config = _configstore.Load())
//            {
//                return config.TagResponses.Keys;
//            }
//        }

//        public void SetTagAndResponse(string tag, string response)
//        {
//            lock (_lock)
//            {
//                using (var config = _configstore.Load())
//                {
//                    config.TagResponses[tag] = response;
//                }
//            }
//        }
//    }
//}
