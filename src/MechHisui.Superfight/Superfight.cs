using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Discord;
using JiiLib;

namespace MechHisui.Superfight
{
    public class Superfight
    {
        private readonly List<Player> _players = new List<Player>();
        private readonly Stack<Card> _characters;
        private readonly Stack<Card> _abilities;
        private readonly Stack<Card> _locations;
        private readonly Channel _channel;

        private readonly Player[] _turnPlayers = new Player[2];

        private GameState _state;
        private int _turn = 0;
        private List<Card> p1Picks;
        private List<Card> p2Picks;
        private List<User> _voters;
        private List<User> _votes;

        public Superfight(List<User> players, Channel channel, string basePath)
        {
            foreach (var user in players)
            {
                _players.Add(new Player(user));
            }

            _channel = channel;
            IEnumerable<string> c = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(Path.Combine(basePath, "sf_chara.json")));
            IEnumerable<string> a = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(Path.Combine(basePath, "sf_ability.json")));
            IEnumerable<string> l = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(Path.Combine(basePath, "sf_location.json")));

            for (int i = 0; i < 28; i++)
            {
                c = c.Shuffle();
                a = a.Shuffle();
                l = l.Shuffle();
            }

            _characters = new Stack<Card>(c.Select(x => new Card(CardType.Character, x)));
            _abilities = new Stack<Card>(a.Select(x => new Card(CardType.Ability, x)));
            _locations = new Stack<Card>(l.Select(x => new Card(CardType.Location, x)));

            _channel.Client.MessageReceived += ProcessMessage;
        }

        public async Task StartGame()
        {
            _state = GameState.Setup;
            for (int i = 0; i < 3; i++)
            {
                foreach (var p in _players)
                {
                    p.Draw(_characters.Pop());
                    p.Draw(_abilities.Pop());
                }
            }

            await StartTurn();
        }

        public void StartVote()
        {
            _state = GameState.Voting;
            _voters = new List<User>();
            _votes  = new List<User>();
        }

        public async Task StartTurn()
        {
            _turn++;
            string str;
            //if (_players.Count > 2)
            //{
                p1Picks = null;
                p2Picks = null;
                var rng = new Random();
                Player rn1;
                Player rn2;
                do
                {
                    rn1 = _players[rng.Next(maxValue: _players.Count)];
                    rn2 = _players[rng.Next(maxValue: _players.Count)];
                } while (rn1.User.Id == rn2.User.Id && (_turnPlayers.Any(u => u.User.Id == rn1.User.Id) && _turnPlayers.Any(u => u.User.Id == rn2.User.Id)));
                _turnPlayers[0] = rn1;
                _turnPlayers[1] = rn2;
                str = $"It is turn {_turn}, the two chosen players are **{_turnPlayers[0].User.Name}** and **{_turnPlayers[1].User.Name}**.";
            //}
            //else
            //{
            //    str = $"It is turn {_turn}.";
            //}

            await _channel.SendWithRetry(str);

            foreach (var p in _turnPlayers)
            {
                p.Draw(_characters.Pop());
                p.Draw(_abilities.Pop());
                await p.SendHand();
            }
            _state = GameState.Choosing;
        }

        private async void ProcessMessage(object sender, MessageEventArgs e)
        {
            if (_players.Select(p => p.User.Id).Contains(e.User.Id))
            {
                await ReallyProcessMessage(sender, e);
            }
        }

        public async Task ReallyProcessMessage(object sender, MessageEventArgs e)
        {
            try
            {
                var pl = _players.SingleOrDefault(p => p.User.Id == e.User.Id);
                if (pl != null && _state == GameState.Choosing
                    && e.Channel.IsPrivate
                    && _turnPlayers.Select(p => p.User.PrivateChannel.Id).Contains(e.Channel.Id))
                {
                    int i;
                    if (Int32.TryParse(e.Message.Text, out i))
                    {
                        if (i > pl.HandSize)
                        {
                            await e.Channel.SendWithRetry("Out of range.");
                            return;
                        }

                        if (!pl.ConfirmedPlay)
                        {
                            await e.Channel.SendWithRetry(pl.ChooseCard(i));
                        }
                    }
                    if (pl.Tentative.Count == 2 && e.Message.Text == ".confirm")
                    {
                        if (_turnPlayers[0].User.Id == pl.User.Id)
                        {
                            p1Picks = pl.Confirm();
                        }
                        else
                        {
                            p2Picks = pl.Confirm();
                        }

                        if (p1Picks != null && p2Picks != null)
                        {
                            var sb = new StringBuilder("Both players have selected their fight:\n")
                                .AppendLine($"{_turnPlayers[0].User.Name}'s fighter: **{p1Picks.Single(c => c.Type == CardType.Character).Text}**")
                                .AppendLine($"with **{p1Picks.Single(c => c.Type == CardType.Ability).Text}** and randomly **{_abilities.Pop().Text}**.")
                                .AppendLine($"{_turnPlayers[1].User.Name}'s fighter: **{p2Picks.Single(c => c.Type == CardType.Character).Text}**")
                                .AppendLine($"with **{p2Picks.Single(c => c.Type == CardType.Ability).Text}** and randomly **{_abilities.Pop().Text}**.")
                                .Append($"And the Arena is **{_locations.Pop().Text}**. Discuss.");
                            await _channel.SendWithRetry(sb.ToString());
                            _state = GameState.Debating;
                        }
                    }
                }
                else if (_state == GameState.Voting && e.Channel.Id == _channel.Id && !_voters.Any(u => u.Id == e.User.Id))
                {
                    if (e.Message.Text.StartsWith(".vote") && e.Message.MentionedUsers.Count() == 1)
                    {
                        var u = e.Message.MentionedUsers.Single();
                        if (!_turnPlayers.Any(p => p.User.Id == u.Id))
                        {
                            await e.Channel.SendWithRetry("That user did not play this turn.");
                            return;
                        }

                        _voters.Add(e.User);
                        _votes.Add(u);

                        if (_voters.Count == _players.Count)
                        {
                            await EndVote();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                //Environment.Exit(0);
            }
        }

        private async Task EndVote()
        {
            var winner = _votes.GroupBy(v => v)
                .Select(v => new { Count = v.Count(), User = v.Key })
                .OrderByDescending(v => v.Count)
                .First();

            await _channel.SendWithRetry($"Voting has ended. The winner is {winner.User.Name}.");
            _players.Single(p => p.User.Id == winner.User.Id).AddPoint();
            await StartTurn();
        }

        public string EndGame()
        {
            _channel.Client.MessageReceived -= ProcessMessage;
            return $"The game ended. The winner is **{_players.OrderByDescending(p => p.Points).First().User.Name}**.";
        }

        public string GetGameState()
        {
            var sb = new StringBuilder($"State of the game at turn {_turn}:\n")
                .AppendLine($"Turn state is {_state.ToString()}.")
                .AppendLine("Players are:");
            foreach (var p in _players)
            {
                if (_turnPlayers.Contains(p))
                {
                    sb.Append($"*{p.User.Name}*");
                }
                else
                {
                    sb.Append(p.User.Name);
                }

                sb.Append($" ({p.Points} points)");

                if (p != _players.Last())
                {
                    sb.Append(", ");
                }
            }
            sb.Append("\n(*Italic* = current turn players.)");

            return sb.ToString();
        }

        private class Player
        {
            public int Points { get; private set; } = 0;
            public User User { get; }
            private readonly List<Card> _hand;
            internal int HandSize => _hand.Count;
            internal List<Card> Tentative;
            internal bool ConfirmedPlay = false;

            public Player(User user)
            {
                User = user;
                _hand = new List<Card>();
            }

            public void Draw(Card card)
            {
                Tentative = new List<Card>();
                _hand.Add(card);
            }

            public async Task SendHand()
            {
                ConfirmedPlay = false;
                int i = 1;
                var sb = new StringBuilder("Your Hand:\n")
                    .AppendSequence(_hand, (b, c) => b.AppendLine($"{i++}: **{c.Type.ToString()}** - {c.Text}"))
                    .Append($"Please pick one {CardType.Character} and one {CardType.Ability} card.");
                await User.CreatePMChannel();
                await User.PrivateChannel.SendWithRetry(sb.ToString());
            }

            public string ChooseCard(int i)
            {
                var tc = _hand.ElementAt(i - 1);
                if (Tentative.Any(c => c.Type == tc.Type))
                {
                    Tentative.RemoveAll(c => c.Type == tc.Type);
                    return $"Replacing your tentative {tc.Type.ToString()} card to `{tc.Text}`";
                }
                else
                {
                    Tentative.Add(tc);
                    return $"Added {tc.Type.ToString()}: `{tc.Text}` to tentative play.";
                }
            }

            public List<Card> Confirm()
            {
                ConfirmedPlay = true;
                _hand.RemoveAll(c => Tentative.Select(t => t.Text).Contains(c.Text));
                return Tentative;
            }

            public void AddPoint() => Points++;
        }

        private class Card
        {
            public CardType Type { get; }
            public string Text { get; }

            public Card(CardType type, string text)
            {
                Type = type;
                Text = text;
            }
        }

        private enum CardType
        {
            Character,
            Ability,
            Location
        }

        private enum GameState
        {
            Setup,
            Choosing,
            Debating,
            Voting
        }
    }
}