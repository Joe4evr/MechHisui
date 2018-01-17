using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;
using MechHisui.Superfight.Models;
using SharedExtensions;
using SharedExtensions.Collections;

namespace MechHisui.Superfight
{
    public sealed class SuperfightGame : GameBase<SuperfightPlayer>
    {
        private readonly Stack<Card> _characters;
        private readonly Stack<Card> _abilities;
        private readonly Stack<Card> _locations;
        private readonly Timer _debateTimer;
        private readonly int _discusstimeout;
        private readonly AsyncObservableCollection<PlayerVote> _votes = new AsyncObservableCollection<PlayerVote>();

        internal readonly SuperfightPlayer[] TurnPlayers = new SuperfightPlayer[2];

        internal GameState State;
        private int _turn = 0;
        private List<Card> _p1Picks;
        private List<Card> _p2Picks;
        //private List<User> _voters;

        public SuperfightGame(
            IMessageChannel channel,
            IEnumerable<SuperfightPlayer> players,
            SuperfightConfig cfg,
            int timeout)
            : base(channel, players, setFirstPlayerImmediately: false)
        {
            var c = cfg.Characters.Shuffle(28);
            var a = cfg.Abilities.Shuffle(28);
            var l = cfg.Locations.Shuffle(28);

            _characters = new Stack<Card>(c.Select(x => new Card(CardType.Character, x)));
            _abilities = new Stack<Card>(a.Select(x => new Card(CardType.Ability, x)));
            _locations = new Stack<Card>(l.Select(x => new Card(CardType.Location, x)));

            _debateTimer = new Timer(
                async cb => await StartVote().ConfigureAwait(false),
                null,
                Timeout.Infinite,
                Timeout.Infinite);
            _discusstimeout = timeout;
        }

        public override Task SetupGame()
        {
            State = GameState.Setup;
            for (int i = 0; i < 3; i++)
            {
                foreach (var p in Players)
                {
                    p.Draw(_characters.Pop());
                    p.Draw(_abilities.Pop());
                }
            }
            return Task.CompletedTask;
        }

        public override Task StartGame()
        {
            return NextTurn();
        }

        public override async Task NextTurn()
        {
            _turn++;
            string str;
            //if (_players.Count > 2)
            //{
                _p1Picks = null;
                _p2Picks = null;
                var rng = new Random();
                SuperfightPlayer rn1;
                SuperfightPlayer rn2;
                do
                {
                    rn1 = Players[rng.Next(maxValue: Players.Count)].Value;
                    rn2 = Players[rng.Next(maxValue: Players.Count)].Value;
                } while (rn1.User.Id == rn2.User.Id
                    && (TurnPlayers.Any(u => u.User.Id == rn1.User.Id)
                    && TurnPlayers.Any(u => u.User.Id == rn2.User.Id)));

                TurnPlayers[0] = rn1;
                TurnPlayers[1] = rn2;
                str = $"It is turn {_turn}, the two chosen players are **{TurnPlayers[0].User.Username}** and **{TurnPlayers[1].User.Username}**.";
            //}
            //else
            //{
            //    str = $"It is turn {_turn}.";
            //}

            await Channel.SendMessageAsync(str).ConfigureAwait(false);

            for (int i = 0; i < TurnPlayers.Length; i++)
            {
                TurnPlayers[i].Draw(_characters.Pop());
                TurnPlayers[i].Draw(_abilities.Pop());
                TurnPlayers[i].ConfirmedPlay = false;
                await TurnPlayers[i].SendHand().ConfigureAwait(false);
            }
            State = GameState.Choosing;
        }

        internal string ChooseInternal(IUser u, int i)
        {
            var player = Players.Single(p => p.User.Id == u.Id);
            if (i > player.HandSize)
            {
                return "Out of range.";
            }

            return !player.ConfirmedPlay
                ? player.ChooseCard(i)
                : "You already confirmed your play.";
        }

        internal async Task ConfirmInternal(IUser u)
        {
            var pl = TurnPlayers.Single(p => p.User.Id == u.Id);
            if (TurnPlayers[0].User.Id == pl.User.Id)
            {
                _p1Picks = pl.Confirm();
            }
            else
            {
                _p2Picks = pl.Confirm();
            }

            if (_p1Picks != null && _p2Picks != null)
            {
                var sb = new StringBuilder("Both players have selected their fight:\n")
                    .AppendLine($"{TurnPlayers[0].User.Username}'s fighter: **{_p1Picks.Single(c => c.Type == CardType.Character).Text}**")
                    .AppendLine($"with **{_p1Picks.Single(c => c.Type == CardType.Ability).Text}** and randomly **{_abilities.Pop().Text}**.")
                    .AppendLine($"{TurnPlayers[1].User.Username}'s fighter: **{_p2Picks.Single(c => c.Type == CardType.Character).Text}**")
                    .AppendLine($"with **{_p2Picks.Single(c => c.Type == CardType.Ability).Text}** and randomly **{_abilities.Pop().Text}**.")
                    .Append($"And the Arena is **{_locations.Pop().Text}**. Discuss.");
                await Channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
                _debateTimer.Change(TimeSpan.FromMinutes(_discusstimeout), Timeout.InfiniteTimeSpan);
                State = GameState.Debating;
            }
        }

        public Task StartVote()
        {
            _debateTimer.Change(Timeout.Infinite, Timeout.Infinite);
            State = GameState.Voting;
            _votes.Clear();
            _votes.CollectionChangedAsync += VotesChanged;
            return Channel.SendMessageAsync("Discussion time is over. Please cast your vote.");
        }

        private async Task VotesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems is IList<PlayerVote> l && l.Count == Players.Count)
                {
                    await EndVote(l).ConfigureAwait(false);
                }
            }
        }

        public Task ProcessVote(IUser voter, IUser target)
        {
            if (!TurnPlayers.Any(p => p.User.Id == target.Id))
            {
                return Channel.SendMessageAsync("That user did not play this turn.");
            }
            if (_votes.Any(v => v.Voter.Id == voter.Id))
            {
                return Channel.SendMessageAsync("You already voted this turn.");
            }

            _votes.Add(new PlayerVote(voter, TurnPlayers.Single(p => p.User.Id == target.Id)));
            return Channel.SendMessageAsync($"**{voter.Username}** cast their vote.");
        }

        private async Task EndVote(IList<PlayerVote> l)
        {
            _votes.CollectionChangedAsync -= VotesChanged;
            State = GameState.VotingClosed;
            var winner = l.GroupBy(v => v.VoteTarget)
                .Select(v => new { Count = v.Count(), Player = v.Key })
                .OrderByDescending(v => v.Count)
                .First();

            await Channel.SendMessageAsync($"Voting has ended. The winner is **{winner.Player.User.Username}**.").ConfigureAwait(false);
            Players.Single(p => p.User.Id == winner.Player.User.Id).AddPoint();
            await NextTurn().ConfigureAwait(false);
        }

        //public string EndGame()
        //{
        //    _channel.Client.MessageReceived -= ProcessMessage;
        //    return $"The game ended. The winner is **{_players.OrderByDescending(p => p.Points).First().User.Name}**.";
        //}

        public override string GetGameState()
        {
            var sb = new StringBuilder($"State of the game at turn {_turn}:\n")
                .AppendLine($"Turn state is {State}.")
                .AppendLine($"Players are: {String.Join(", ", Players.Select(FormatPlayer))}");
            sb.Append("\n(*Italic* = current turn players.)");

            return sb.ToString();

            string FormatPlayer(SuperfightPlayer player)
            {
                return TurnPlayers.Contains(player)
                    ? $"*{player.User.Username}* ({player.Points} points)"
                    : $"{player.User.Username} ({player.Points} points)";
            }
        }
    }
}