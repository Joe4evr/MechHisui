using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;

namespace DivaBot
{
    public class ScoreAttackChallenge
    {
        public Dictionary<string, string> Titles { get; set; }
        //public string Title { get; set; }
        //public string JpTitle { get; set; }
        public DateTime ExpiresOn { get; set; }

        [JsonConverter(typeof(CaseInvariantKeyDictionaryConverter<Dictionary<ulong, string>>))]
        public Dictionary<string, Dictionary<ulong, string>> Scores { get; set; }

        [JsonIgnore]
        internal Timer _timer;
    }
}
