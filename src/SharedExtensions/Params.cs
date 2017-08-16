using System;
using Discord;

namespace SharedExtensions
{
    internal sealed class Params
    {
        public string ConfigPath { get; private set; }
        public LogSeverity? LogSeverity { get; private set; }
        public int? Shards { get; private set; }
        public int? ShardId { get; private set; }
        public GetOnce<string> Token { get; private set; }

        private Params()
        {
        }

        public static Params Parse(string[] parameters)
        {
            var p = new Params();
            for (int i = 0; i < parameters.Length; i++)
            {
                switch (parameters[i])
                {
                    case "-c":
                    case "-config":
                        p.ConfigPath = parameters[i + 1];
                        break;
                    case "-l":
                    case "-log":
                        p.LogSeverity = (LogSeverity)Enum.Parse(typeof(LogSeverity), parameters[i + 1], ignoreCase: true);
                        break;
                    case "-s":
                    case "-shards":
                        p.Shards = Int32.Parse(parameters[i + 1]);
                        break;
                    case "-sid":
                    case "-shardid":
                        p.ShardId = Int32.Parse(parameters[i + 1]);
                        break;
                    case "-t":
                        p.Token = new GetOnce<string>(parameters[i + 1]);
                        break;
                    default:
                        continue;
                }
            }
            return p;
        }

        /// <summary>Wraps an object to be gettable only once.</summary>
        internal sealed class GetOnce<T>
        {
            private T _item;
            private bool _gotten = false;

            public GetOnce(T item)
            {
                _item = item;
            }

            /// <summary>Gets and clears the inner object.</summary>
            /// <exception cref="InvalidOperationException">The inner object was already cleared.</exception>
            public T GetAndClear()
            {
                if (_gotten) throw new InvalidOperationException("Item is already cleared.");

                var i = _item;
                _item = default(T);
                _gotten = true;
                return i;
            }
        }
    }
}
