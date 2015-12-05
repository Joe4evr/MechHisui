using Discord;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace MechHisui.Modules
{
    public class Trivia
    {
        private readonly DiscordClient _client;
        private readonly int _rounds;
        public Channel Channel { get; }
        private readonly ConcurrentDictionary<User, int> _scoreboard;
        private readonly List<string> _asked;
        private readonly Random _rng;
        private bool _isAnswered = false;
        private KeyValuePair<string, string[]> _currentQuestion;
        private TriviaType _type;
        private Timer _timer;
        
        public Trivia(DiscordClient client, int rounds, Channel channel, TriviaType type = TriviaType.WinAt)
        {
            _client = client;
            _client.MessageReceived += CheckTrivia;
            _rounds = rounds;
            _type = type;
            _asked = new List<string>();
            _rng = new Random();
            _client.SendMessage(Channel, $"Trivia commencing. *Start the clock!*");
        }

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        public void StartTrivia() => AskQuestion();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        private async void TimeUp(object sender, ElapsedEventArgs e)
        {
            _isAnswered = true;
            await _client.SendMessage(Channel, $"Time up. Next question commencing in 15 seconds.");
            await Task.Delay(TimeSpan.FromSeconds(15));
            await AskQuestion();
        }

        public async Task EndTrivia()
        {
            _client.MessageReceived -= CheckTrivia;
            await _client.SendMessage(Channel, $"Aborting trivia. {_scoreboard.OrderByDescending(kv => kv.Value).First().Key.Name} has the most points.");
        }

        private async Task EndTrivia(User winner)
        {
            _client.MessageReceived -= CheckTrivia;
            await _client.SendMessage(Channel, $"Trivia over, {winner.Name} has won.");
            _client.GetTrivias().Remove(this);
        }

        private async Task AskQuestion()
        {
            do
            {
                _currentQuestion = TriviaHelpers.Questions.ElementAt(_rng.Next() % TriviaHelpers.Questions.Count);
            } while (_asked.Contains(_currentQuestion.Key));

            _asked.Add(_currentQuestion.Key);
            _isAnswered = false;
            await _client.SendMessage(Channel, _currentQuestion.Key);
            _timer = new Timer(TimeSpan.FromSeconds(90).TotalMilliseconds)
            {
                AutoReset = false,
                Enabled = true
            };
            _timer.Elapsed += TimeUp;
        }
        
        private async void CheckTrivia(object sender, MessageEventArgs e)
        {
            if (e.Channel == Channel && !_isAnswered && _currentQuestion.Value.Contains(e.Message.Text.ToLowerInvariant()))
            {
                _isAnswered = true;
                _scoreboard.AddOrUpdate(e.User, 1, (k, v) => v++);
                var userScore = _scoreboard.Single(kv => kv.Key == e.User).Value;
                await _client.SendMessage(Channel, $"Correct. {e.User.Name} is now at {userScore} points.");
                if (_type == TriviaType.WinAt && userScore == _rounds)
                {
                    await EndTrivia(e.User);
                }
                //else if (_type == TriviaType.BestOf && userScore ==)
                //{
                //
                //}
                else
                {
                    await _client.SendMessage(Channel, $"Next question commencing in 15 seconds.");
                    await Task.Delay(TimeSpan.FromSeconds(15));
                    await AskQuestion();
                }
            }
        }

        public enum TriviaType
        {
            //BestOf,
            WinAt
        }
    }

    internal static class TriviaHelpers
    {
        internal static IReadOnlyDictionary<string, string[]> Questions = new Dictionary<string, string[]>()
        {
            { "", new[] { "" } },
            { "", new[] { "" } }
        };
    }
}
