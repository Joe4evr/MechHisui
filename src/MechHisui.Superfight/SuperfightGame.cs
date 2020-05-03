using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;
using SharedExtensions;
using SharedExtensions.Collections;
using MechHisui.Superfight.Models;

namespace MechHisui.Superfight
{
    public sealed class SuperfightGame : GameBase<SuperfightPlayer>
    {
        private readonly SuperfightDeck<CharacterCard> _characters;
        private readonly SuperfightDeck<AbilityCard>   _abilities;
        private readonly SuperfightDeck<LocationCard>  _locations;
        private readonly Timer _debateTimer;
        private readonly int _discusstimeout;
        private readonly AsyncObservableCollection<PlayerVote> _votes = new AsyncObservableCollection<PlayerVote>();

        internal readonly SuperfightPlayer[] TurnPlayers = new SuperfightPlayer[2];

        internal GameState State;
        private int _turn = 0;
        private IReadOnlyList<ISuperfightCard>? _p1Picks;
        private IReadOnlyList<ISuperfightCard>? _p2Picks;
        //private List<User> _voters;

        public SuperfightGame(
            IMessageChannel channel, IEnumerable<SuperfightPlayer> players,
            ISuperfightConfig config, int timeout)
            : base(channel, players, setFirstPlayerImmediately: false)
        {
            var allCards = config.GetAllCards().ToLookup(c => c.Type);
            _characters = new SuperfightDeck<CharacterCard>(allCards[CardType.Character].Shuffle(28).Select(c => new CharacterCard(c.Text)));
            _abilities  = new SuperfightDeck<AbilityCard>  (allCards[CardType.Ability]  .Shuffle(28).Select(c => new AbilityCard  (c.Text)));
            _locations  = new SuperfightDeck<LocationCard> (allCards[CardType.Location] .Shuffle(28).Select(c => new LocationCard (c.Text)));

            _debateTimer = new Timer(async _ => await StartVote().ConfigureAwait(false),
                null, Timeout.Infinite, Timeout.Infinite);
            _discusstimeout = timeout;
        }

        public override Task SetupGame()
        {
            State = GameState.Setup;
            for (int i = 0; i < 3; i++)
            {
                foreach (var p in Players)
                {
                    p.Draw(_characters.Draw());
                    p.Draw(_abilities.Draw());
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
                TurnPlayers[i].Draw(_characters.Draw());
                TurnPlayers[i].Draw(_abilities.Draw());
                TurnPlayers[i].ConfirmedPlay = false;
                await TurnPlayers[i].SendHand().ConfigureAwait(false);
            }
            State = GameState.Choosing;
        }

        internal string ChooseInternal(SuperfightPlayer player, int i)
        {
            if (i > player.HandSize)
            {
                return "Out of range.";
            }

            return !player.ConfirmedPlay
                ? player.ChooseCard(i)
                : "You already confirmed your play.";
        }

        internal async Task ConfirmInternal(SuperfightPlayer player)
        {
            if (TurnPlayers[0].User.Id == player.User.Id)
            {
                _p1Picks = player.Confirm();
            }
            else
            {
                _p2Picks = player.Confirm();
            }

            if (_p1Picks != null && _p2Picks != null)
            {
                await Showdown().ConfigureAwait(false);
            }
        }

        private Board? _board;

        private async Task Showdown()
        {
            var location = _locations.Draw();
            var p1f = (_p1Picks.Single(c => c.Type == CardType.Character) as CharacterCard)!;
            var p1a = (_p1Picks.Single(c => c.Type == CardType.Ability) as AbilityCard)!;
            var p1ra = _abilities.Draw();
            var p2f = (_p2Picks.Single(c => c.Type == CardType.Character) as CharacterCard)!;
            var p2a = (_p2Picks.Single(c => c.Type == CardType.Ability) as AbilityCard)!;
            var p2ra = _abilities.Draw();

            _board = new Board(location, p1f, p2f)
            {
                Fighter1Abilities = { p1a, p1ra },
                Fighter2Abilities = { p2a, p2ra }
            };

            var sb = new StringBuilder("Both players have selected their fight:\n")
                .AppendLine($"{TurnPlayers[0].User.Username}'s fighter: **{p1f.Text}**")
                .AppendLine($"with **{p1a.Text}** and randomly **{p1ra.Text}**.")
                .AppendLine($"{TurnPlayers[1].User.Username}'s fighter: **{p2f.Text}**")
                .AppendLine($"with **{p2a.Text}** and randomly **{p2ra.Text}**.")
                .Append($"And the Arena is **{location.Text}**. Discuss.");

            await Channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
            _debateTimer.Change(TimeSpan.FromMinutes(_discusstimeout), Timeout.InfiniteTimeSpan);
            State = GameState.Debating;
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

        public Task ProcessVote(SuperfightPlayer voter, SuperfightPlayer target)
        {
            if (!TurnPlayers.Any(p => p.User.Id == target.User.Id))
            {
                return Channel.SendMessageAsync("That user did not play this turn.");
            }
            if (_votes.Any(v => v.Voter.User.Id == voter.User.Id))
            {
                return Channel.SendMessageAsync("You already voted this turn.");
            }

            _votes.Add(new PlayerVote(voter, TurnPlayers.Single(p => p.User.Id == target.User.Id)));
            return Channel.SendMessageAsync($"**{voter.User.Username}** cast their vote.");
        }

        private async Task EndVote(IList<PlayerVote> votes)
        {
            _votes.CollectionChangedAsync -= VotesChanged;
            State = GameState.VotingClosed;
            var p1votes = votes.Count(v => v.VoteTarget.User.Id == TurnPlayers[0].User.Id);
            var p2votes = votes.Count(v => v.VoteTarget.User.Id == TurnPlayers[1].User.Id);

            if (p1votes == p2votes)
            {
                await Channel.SendMessageAsync($"The votes are tied. Both fighters will receive an additional random ability....").ConfigureAwait(false);

                var f1r = _abilities.Draw();
                var f2r = _abilities.Draw();

                var board = _board!;

                var sb = new StringBuilder("Current state:\n")
                    .AppendLine($"{TurnPlayers[0].User.Username}'s fighter: **{board.Fighter1.Text}**")
                    .AppendLine($"with abilities: **{String.Join("**, **", board.Fighter1Abilities.Select(a => a.Text))}**")
                    .AppendLine($"and new random: **{f1r.Text}**.")
                    .AppendLine($"{TurnPlayers[1].User.Username}'s fighter: **{board.Fighter2.Text}**")
                    .AppendLine($"with abilities: **{String.Join("**, **", board.Fighter2Abilities.Select(a => a.Text))}**")
                    .AppendLine($"and new random: **{f2r.Text}**.")
                    .Append($"Fighting at **{board.Location.Text}**. Discuss again.");

                board.Fighter1Abilities.Add(f1r);
                board.Fighter2Abilities.Add(f2r);

                await Channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
                _debateTimer.Change(TimeSpan.FromMinutes(_discusstimeout), Timeout.InfiniteTimeSpan);
                State = GameState.Debating;
                return;
            }

            var winner = (p1votes > p2votes) ? TurnPlayers[0] : TurnPlayers[1];

            await Channel.SendMessageAsync($"Voting has ended. The winner is **{winner.User.Username}**.").ConfigureAwait(false);
            winner.AddPoint();
            _board = null;
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

        public override Embed GetGameStateEmbed() => throw new NotImplementedException();
    }
}