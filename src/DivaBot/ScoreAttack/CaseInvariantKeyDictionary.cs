using System;
using System.Collections.Generic;
using Newtonsoft.Json.Converters;

namespace DivaBot
{
    internal class CaseInvariantKeyDictionaryConverter<TValue> : CustomCreationConverter<Dictionary<string, TValue>>
    {
        public override Dictionary<string, TValue> Create(Type objectType)
        {
            return new Dictionary<string, TValue>(StringComparer.OrdinalIgnoreCase);
        }
    }
}