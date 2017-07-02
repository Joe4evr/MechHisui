using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;
using MechHisui.SecretHitler.Models;
using SharedExtensions.Collections;

namespace MechHisui.SecretHitler
{
    public sealed class SecretHitlerGame : GameBase<SecretHitlerPlayer>
    {
        private readonly Queue<BoardSpace> _liberalTrack;
        private readonly Queue<BoardSpace> _fascistTrack;
        private readonly SecretHitlerConfig _config;
        private readonly HouseRules _houseRules;
        private readonly AsyncObservableCollection<PlayerVote> _votes = new AsyncObservableCollection<PlayerVote>();
        private readonly List<PolicyType> _policies = new List<PolicyType>();
        private readonly Stack<PolicyType> _discards = new Stack<PolicyType>();
        private readonly List<SecretHitlerPlayer> _confirmedNot = new List<SecretHitlerPlayer>();

        private int _turn = 0;
        private int _electionTracker = 0;
        private bool _takenVeto = false;

        private Stack<PolicyType> _deck;
        private Node<SecretHitlerPlayer> _afterSpecial;
        private SecretHitlerPlayer _lastPresident;
        private SecretHitlerPlayer _chancellorNominee;
        private SecretHitlerPlayer _lastChancellor;
        private SecretHitlerPlayer _specialElected;

        private IEnumerable<SecretHitlerPlayer> LivingPlayers => Players.Where(p => p.IsAlive);

        internal GameState State { get; private set; }
        internal SecretHitlerPlayer CurrentChancellor { get; private set; }
        internal SecretHitlerPlayer CurrentPresident => TurnPlayer.Value;
        internal bool VetoUnlocked { get; private set; } = false;

        internal SecretHitlerGame(
            IMessageChannel channel,
            IEnumerable<SecretHitlerPlayer> players,
            SecretHitlerConfig config,
            HouseRules houseRules)
            : base(channel, players)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _houseRules = houseRules;

            State = GameState.Setup;

            _liberalTrack = new Queue<BoardSpace>();
            _liberalTrack.Enqueue(new BoardSpace(BoardSpaceType.Blank));
            _liberalTrack.Enqueue(new BoardSpace(BoardSpaceType.Blank));
            _liberalTrack.Enqueue(new BoardSpace(BoardSpaceType.Blank));
            _liberalTrack.Enqueue(new BoardSpace(BoardSpaceType.Blank));
            _liberalTrack.Enqueue(new BoardSpace(BoardSpaceType.LiberalWin));

            _fascistTrack = new Queue<BoardSpace>();

            switch (Players.Count)
            {
                case 5:
                case 6:
                    _fascistTrack.Enqueue(new BoardSpace(BoardSpaceType.Blank));
                    _fascistTrack.Enqueue(new BoardSpace(BoardSpaceType.Blank));
                    _fascistTrack.Enqueue(new BoardSpace(BoardSpaceType.Examine));
                    break;
                case 7:
                case 8:
                    _fascistTrack.Enqueue(new BoardSpace(BoardSpaceType.Blank));
                    _fascistTrack.Enqueue(new BoardSpace(BoardSpaceType.Investigate));
                    _fascistTrack.Enqueue(new BoardSpace(BoardSpaceType.ChooseNextCandidate));
                    break;
                case 9:
                case 10:
                    _fascistTrack.Enqueue(new BoardSpace(BoardSpaceType.Investigate));
                    _fascistTrack.Enqueue(new BoardSpace(BoardSpaceType.Investigate));
                    _fascistTrack.Enqueue(new BoardSpace(BoardSpaceType.ChooseNextCandidate));
                    break;
                default:
                    break;
            }
            _fascistTrack.Enqueue(new BoardSpace(BoardSpaceType.Execution));
            _fascistTrack.Enqueue(new BoardSpace(BoardSpaceType.ExecutionVeto));
            _fascistTrack.Enqueue(new BoardSpace(BoardSpaceType.FascistWin));
        }

        public override Task SetupGame()
        {
            _deck = new Stack<PolicyType>(
                Enumerable.Repeat(PolicyType.Fascist, 11)
                .Concat(Enumerable.Repeat(PolicyType.Liberal, 6)));

            _deck = new Stack<PolicyType>(_deck.Shuffle(32));

            return Task.CompletedTask;
        }

        public override async Task StartGame()
        {
            foreach (var player in Players)
            {
                var otherFacists = Players.Where(p =>
                        p.Party == _config.FascistParty
                        && p.User.Id != player.User.Id
                        && p.Role != _config.Hitler).ToList();

                var sb = new StringBuilder($"Your party is **{player.Party}**. ")
                    .AppendLine($"Your role is **{player.Role}**.")
                    .AppendWhen(() => player.Role == _config.Hitler, b =>
                        otherFacists.Count == 1
                        ? b.Append($"Your teammate is **{otherFacists.Single().User.Username}**. Use this information wisely.")
                        : b.Append("Guard your secret well."))
                    .AppendWhen(() => player.Party == _config.FascistParty && player.Role != _config.Hitler, b =>
                    {
                        if (otherFacists.Count == 1)
                        {
                            return b.Append($"{_config.Hitler} is **{otherFacists.Single().User.Username}**. Use this information wisely.");
                        }
                        else
                        {
                            b.AppendLine($"Your teammates are **{String.Join("**, **", otherFacists.Select(p => p.User.Username))}**.");
                            return b.Append($"{_config.Hitler} is **{Players.Single(p => p.Role == _config.Hitler).User.Username}**. Use this information wisely.");
                        }
                    });

                await player.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
                await Task.Delay(1000).ConfigureAwait(false);
            }
            await Channel.SendMessageAsync($"The order of players is: {String.Join(" -> ", Players.Select(p => p.User.Username))}").ConfigureAwait(false);
            await NextTurn().ConfigureAwait(false);
        }

        public override async Task NextTurn()
        {
            _turn++;
            State = GameState.StartOfTurn;
            _takenVeto = false;
            if (_specialElected == null)
            {
                do TurnPlayer = TurnPlayer.Next;
                while (!CurrentPresident.IsAlive);
            }
            else if (_afterSpecial == null)
            {
                _afterSpecial = TurnPlayer.Next;
                TurnPlayer = Players.Find(_specialElected);
            }
            else
            {
                TurnPlayer = (_afterSpecial.Value.User.Id != CurrentPresident.User.Id)
                    ? _afterSpecial
                    : TurnPlayer.Next;

                _specialElected = null;
                _afterSpecial = null;
            }
            CurrentChancellor = null;
            await Channel.SendMessageAsync($"It is turn {_turn}, the {_config.President} is **{CurrentPresident.User.Username}**. Please choose your {_config.Chancellor}.").ConfigureAwait(false);
        }

        public async Task NominatedChancellor(IUser nominee)
        {
            _chancellorNominee = Players.Single(p => p.User.Id == nominee.Id);
            if (_turn == 1 && (_houseRules & HouseRules.SkipFirstElection) == HouseRules.SkipFirstElection)
            {
                await Channel.SendMessageAsync($"Because of the applied house rule, this vote will be skipped.").ConfigureAwait(false);
                CurrentChancellor = _chancellorNominee;
                await DrawPolicies().ConfigureAwait(false);
                return;
            }
            if (_chancellorNominee.User.Id == CurrentPresident.User.Id)
            {
                await Channel.SendMessageAsync($"**{nominee.Username}** is the current {_config.President} last turn and is therefore ineligible.").ConfigureAwait(false);
                return;
            }
            if (_chancellorNominee.User.Id == _lastChancellor.User.Id)
            {
                await Channel.SendMessageAsync($"**{nominee.Username}** has been the {_config.Chancellor} last turn and is therefore ineligible.").ConfigureAwait(false);
                return;
            }
            if (_chancellorNominee.User.Id == _lastPresident.User.Id && Players.Count > 5)
            {
                await Channel.SendMessageAsync($"**{nominee.Username}** has been the {_config.President} last turn and is therefore ineligible.").ConfigureAwait(false);
                return;
            }
            if (!_chancellorNominee.IsAlive)
            {
                await Channel.SendMessageAsync($"**{nominee.Username}** is dead and is therefore ineligible.").ConfigureAwait(false);
                return;
            }

            State = GameState.VoteForGovernment;
            _votes.Clear();
            _votes.CollectionChangedAsync += VotesChanged;
            await Channel.SendMessageAsync($"**{CurrentPresident.User.Username}** has nominated **{nominee.Username}** as their {_config.Chancellor}. PM me `{_config.Yes}` or `{_config.No}` to vote on this proposal.").ConfigureAwait(false);
            foreach (var player in Players)
            {
                await player.SendMessageAsync($"**{CurrentPresident.User.Username}** nominated **{nominee.Username}** as {_config.Chancellor}. Do you agree?").ConfigureAwait(false);
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }

        public async Task ProcessVote(IDMChannel dms, IUser user, string vote)
        {
            if (!_votes.Any(p => p.User.Id == user.Id)
                && LivingPlayers.Any(p => p.User.Id == user.Id))
            {
                Vote v;
                if (vote.Equals(_config.Yes, StringComparison.OrdinalIgnoreCase))
                {
                    v = Vote.Yes;
                }
                else if (vote.Equals(_config.No, StringComparison.OrdinalIgnoreCase))
                {
                    v = Vote.No;
                }
                else
                {
                    await dms.SendMessageAsync("Unacceptable parameter.").ConfigureAwait(false);
                    return;
                }
                _votes.Add(new PlayerVote(user, v));

                await dms.SendMessageAsync("Your vote has been recorded.").ConfigureAwait(false);
            }
        }

        public async Task ChancellorVetos(IDMChannel dms)
        {
            if (!_takenVeto)
            {
                State = GameState.ChancellorVetod;
                await Channel.SendMessageAsync($"The {_config.Chancellor} has opted to veto. Do you consent, Mr./Mrs. {_config.President}?").ConfigureAwait(false);
                return;
            }
            else
            {
                await dms.SendMessageAsync($"You have already attempted to veto.").ConfigureAwait(false);
                return;
            }
        }

        public async Task PresidentConsentsVeto(string consent)
        {
            if (!_takenVeto)
            {
                if (consent.Equals("approved", StringComparison.OrdinalIgnoreCase))
                {
                    await Channel.SendMessageAsync($"The {_config.President} has approved the {_config.Chancellor}'s veto. Next turn when players are ready.").ConfigureAwait(false);
                    foreach (var p in _policies)
                    {
                        _discards.Push(p);
                    }
                    _policies.Clear();
                    _electionTracker++;
                    _takenVeto = true;
                    State = GameState.EndOfTurn;
                }
                else if (consent.Equals("denied", StringComparison.OrdinalIgnoreCase))
                {
                    await Channel.SendMessageAsync($"The {_config.President} has denied veto and the {_config.Chancellor} must play.").ConfigureAwait(false);
                    State = GameState.ChancellorPicks;
                    _takenVeto = true;
                }
                else
                {
                    await Channel.SendMessageAsync("Unacceptable parameter.").ConfigureAwait(false);
                }
            }
        }

        public async Task SpecialElection(IUser player)
        {
            if (player != null && LivingPlayers.Any(p =>
                p.User.Id != CurrentChancellor.User.Id
                && p.User.Id != CurrentPresident.User.Id
                && p.User.Id == player.Id))
            {
                State = GameState.EndOfTurn;
                _specialElected = Players.Single(p => p.User.Id == player.Id);
                await Channel.SendMessageAsync($"The {_config.President} has Special Elected **{player.Username}**.").ConfigureAwait(false);
            }
            else
            {
                await Channel.SendMessageAsync("Ineligible for nomination.").ConfigureAwait(false);
            }
        }

        public async Task KillPlayer(IUser target)
        {
            if (target != null && LivingPlayers.Any(p => p.User.Id == target.Id))
            {
                State = GameState.EndOfTurn;
                var player = LivingPlayers.Single(p => p.User.Id == target.Id);
                player.Killed();
                await Channel.SendMessageAsync(String.Format(_config.Kill, player.User.Username)).ConfigureAwait(false);
                await Task.Delay(3500).ConfigureAwait(false);
                if (player.Role == _config.Hitler)
                {
                    await EndGame(String.Format(_config.HitlerWasKilled, _config.Hitler, _config.LiberalParty)).ConfigureAwait(false);
                }
                else
                {
                    _confirmedNot.Add(player);
                    await Channel.SendMessageAsync(String.Format(_config.HitlerNotKilled, player.User.Username, _config.Hitler)).ConfigureAwait(false);
                }
            }
        }

        public async Task InvestigatePlayer(IUser target)
        {
            if (target != null && LivingPlayers.Any(p => p.User.Id == target.Id))
            {
                State = GameState.EndOfTurn;
                await Channel.SendMessageAsync($"The {_config.President} is investigating **{target.Username}**'s loyalty.").ConfigureAwait(false);
                await Task.Delay(1000).ConfigureAwait(false);
                var player = LivingPlayers.Single(p => p.User.Id == target.Id);
                await CurrentPresident.SendMessageAsync($"**{player.User.Username}** belongs to the **{player.Party}**. You are not required to answer truthfully.").ConfigureAwait(false);
            }
        }

        private async Task VotesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add
                && e.NewItems is IList<PlayerVote> l
                && l.Count == Players.Count(p => p.IsAlive))
            {
                await VotingResults(l).ConfigureAwait(false);
            }
        }

        private async Task VotingResults(IList<PlayerVote> votes)
        {
            _votes.CollectionChangedAsync -= VotesChanged;
            State = GameState.VotingClosed;
            int favored = votes.Count(v => v.Vote == Vote.Yes);
            int opposed = votes.Count(v => v.Vote == Vote.No);
            var sb = new StringBuilder($"The results are in.\n")
                .AppendSequence(votes, (b, vote) =>
                    b.AppendLine($"**{vote.User.Username}**: {(vote.Vote == Vote.Yes ? _config.Yes : _config.No)}"))
                .AppendLine($"Total in favor: {favored} - Total opposed: {opposed}");

            if (opposed >= favored)
            {
                _electionTracker++;
                sb.Append($"The vote has **not** gone through. {_config.Parliament} is stalled and ");
                switch (_electionTracker)
                {
                    case 1:
                        sb.Append(_config.ThePeopleOne);
                        await Channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
                        break;
                    case 2:
                        sb.Append(_config.ThePeopleTwo);
                        await Channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
                        break;
                    case 3:
                        sb.AppendLine(_config.ThePeopleThree);
                        await Channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
                        await Task.Delay(5000).ConfigureAwait(false);
                        _electionTracker = 0;
                        var pol = _deck.Pop();
                        await Channel.SendMessageAsync(String.Format(_config.ThePeopleEnacted, pol.ToString())).ConfigureAwait(false);
                        if (pol == PolicyType.Fascist)
                        {
                            await ResolveEffect(_fascistTrack.Dequeue(), peopleEnacted: true).ConfigureAwait(false);
                        }
                        else
                        {
                            await ResolveEffect(_liberalTrack.Dequeue(), peopleEnacted: true).ConfigureAwait(false);
                        }
                        if (_deck.Count == 0)
                        {
                            ReshuffleDeck();
                        }
                        break;
                }
            }
            else
            {
                _lastPresident = CurrentPresident;
                CurrentChancellor = _chancellorNominee;
                _lastChancellor = _chancellorNominee;
                _electionTracker = 0;
                if (_fascistTrack.Count() < 3 && !_confirmedNot.Any(u => u.User.Id == CurrentChancellor.User.Id))
                {
                    sb.Append($"The vote has gone through. Now to ask: **Are you {_config.Hitler}**?");
                    await Channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                    var chosen = CurrentChancellor.User;
                    if (Players.Single(p => p.Role == _config.Hitler).User.Id == chosen.Id)
                    {
                        await Channel.SendMessageAsync($"**{chosen.Username}** is, in fact, {_config.Hitler}.").ConfigureAwait(false);
                        await EndGame(_config.FascistsWin).ConfigureAwait(false);
                    }
                    else
                    {
                        _confirmedNot.Add(CurrentChancellor);
                        await Channel.SendMessageAsync($"**{chosen.Username}** is Not {_config.Hitler} (Confirmed). {_config.Parliament} can function.").ConfigureAwait(false);
                        await DrawPolicies().ConfigureAwait(false);
                    }
                }
                else
                {
                    sb.Append($"The vote has gone through. {_config.Parliament} can function.");
                    await Channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
                    await DrawPolicies().ConfigureAwait(false);
                }
            }
        }

        private Task DrawPolicies()
        {
            State = GameState.PresidentPicks;

            _policies.Clear();
            _policies.AddRange(new[] { _deck.Pop(), _deck.Pop(), _deck.Pop() });
            var sb = new StringBuilder($"You have drawn the following {_config.Policies}:\n");
            for (int i = 0; i < _policies.Count; i++)
            {
                sb.AppendLine($"\t**{i + 1}**: {(_policies[i] == PolicyType.Fascist ? _config.Fascist : _config.Liberal)}");
            }
            sb.Append($"Which {_config.Policy} will you discard? The other two are automatically sent to your {_config.Chancellor}.");

            return CurrentPresident.SendMessageAsync(sb.ToString());
        }

        public async Task PresidentDiscards(IDMChannel dms, int nr)
        {
            int pick = nr - 1;
            var tmp = _policies[pick];
            await dms.SendMessageAsync($"Removing a {(tmp == PolicyType.Fascist ? _config.Fascist : _config.Liberal)} {_config.Policy}.").ConfigureAwait(false);
            _discards.Push(tmp);
            _policies.RemoveAt(pick);
            await Channel.SendMessageAsync($"The {_config.President} has discarded one {_config.Policy}.").ConfigureAwait(false);
            await Task.Delay(1000).ConfigureAwait(false);
            await ChancellorPick().ConfigureAwait(false);
        }

        private Task ChancellorPick()
        {
            State = GameState.ChancellorPicks;
            var sb = new StringBuilder($"The {_config.President} has given you these {_config.Policies}:");
            for (int i = 0; i < _policies.Count; i++)
            {
                sb.AppendLine($"\t**{i + 1}**: {(_policies[i] == PolicyType.Fascist ? _config.Fascist : _config.Liberal)}");
            }

            if (!VetoUnlocked)
            {
                sb.Append($"Which {_config.Policy} will you play?");
            }
            else
            {
                sb.Append($"Will you play one or `veto`?");
            }
            return CurrentChancellor.SendMessageAsync(sb.ToString());
        }

        public async Task ChancellorPlays(IDMChannel dms, int nr)
        {
            int pick = nr - 1;
            var tmp = _policies[pick];
            _policies.RemoveAt(pick);
            await dms.SendMessageAsync($"Playing a {(tmp == PolicyType.Fascist ? _config.Fascist : _config.Liberal)} {_config.Policy}.").ConfigureAwait(false);
            _discards.Push(_policies.Single());
            await Channel.SendMessageAsync($"The {_config.Chancellor} has discarded one {_config.Policy} and played a **{(tmp == PolicyType.Fascist ? _config.Fascist : _config.Liberal)}** {_config.Policy}.").ConfigureAwait(false);
            await Task.Delay(1000).ConfigureAwait(false);
            var space = (tmp == PolicyType.Fascist)
                ? _fascistTrack.Dequeue()
                : _liberalTrack.Dequeue();
            await ResolveEffect(space).ConfigureAwait(false);
            if (_deck.Count < 3)
            {
                ReshuffleDeck();
            }
            _policies.Clear();
        }

        private async Task ResolveEffect(BoardSpace space, bool peopleEnacted = false)
        {
            State = GameState.PolicyEnacted;

            if (peopleEnacted)
            {
                switch (space.BoardSpaceType)
                {
                    case BoardSpaceType.ExecutionVeto:
                        VetoUnlocked = true;
                        await Channel.SendMessageAsync($"The {_config.President} and {_config.Chancellor} may veto from now.").ConfigureAwait(false);
                        return;
                    case BoardSpaceType.FascistWin:
                        await EndGame(_config.FascistsWin).ConfigureAwait(false);
                        return;
                    case BoardSpaceType.LiberalWin:
                        await EndGame(_config.LiberalsWin).ConfigureAwait(false);
                        return;
                    default:
                        await Channel.SendMessageAsync($"No action is taken.").ConfigureAwait(false);
                        return;
                }
            }
            else
            {
                switch (space.BoardSpaceType)
                {
                    case BoardSpaceType.Blank:
                        await Channel.SendMessageAsync($"Nothing happens. Next turn when players are ready.").ConfigureAwait(false);
                        return;
                    case BoardSpaceType.Examine:
                        await Channel.SendMessageAsync($"The {_config.President} may see the top 3 cards from the {_config.Policy} deck.").ConfigureAwait(false);
                        string tops = String.Join("**, **", new[]
                        {
                            _deck.ElementAt(0) == PolicyType.Fascist ? _config.Fascist : _config.Liberal,
                            _deck.ElementAt(1) == PolicyType.Fascist ? _config.Fascist : _config.Liberal,
                            _deck.ElementAt(2) == PolicyType.Fascist ? _config.Fascist : _config.Liberal
                        });
                        await CurrentPresident.SendMessageAsync($"The top 3 {_config.Policy} cards are **{tops}**").ConfigureAwait(false);
                        return;
                    case BoardSpaceType.Investigate:
                        State = GameState.Investigating;
                        await Channel.SendMessageAsync($"The {_config.President} may investigate one player's Party affinity.").ConfigureAwait(false);
                        return;
                    case BoardSpaceType.ChooseNextCandidate:
                        State = GameState.SpecialElection;
                        await Channel.SendMessageAsync($"The current {_config.President} may choose the next turn's {_config.President}.").ConfigureAwait(false);
                        return;
                    case BoardSpaceType.Execution:
                        State = GameState.Kill;
                        await Channel.SendMessageAsync($"The {_config.President} may choose another player to kill.").ConfigureAwait(false);
                        return;
                    case BoardSpaceType.ExecutionVeto:
                        VetoUnlocked = true;
                        State = GameState.Kill;
                        await Channel.SendMessageAsync($"The {_config.President} may choose another player to kill. Also, the {_config.President} and {_config.Chancellor} may veto.").ConfigureAwait(false);
                        return;
                    case BoardSpaceType.FascistWin:
                        await EndGame(_config.FascistsWin).ConfigureAwait(false);
                        return;
                    case BoardSpaceType.LiberalWin:
                        await EndGame(_config.LiberalsWin).ConfigureAwait(false);
                        return;
                }
            }
        }

        public override Task EndGame(string endmsg)
        {
            var sb = new StringBuilder(endmsg)
                .AppendLine("\nThe game is over.")
                .AppendLine($"The {_config.Fascist}s were **{String.Join("**, **", Players.Where(p => p.Party == _config.FascistParty).Select(p => p.User.Username))}**.")
                .AppendLine($"The {_config.Liberal}s were **{String.Join("**, **", Players.Where(p => p.Party == _config.LiberalParty).Select(p => p.User.Username))}**.");
            return base.EndGame(sb.ToString());
        }

        private void ReshuffleDeck()
        {
            var temp = _deck.Concat(_discards);
            _deck = new Stack<PolicyType>(temp.Shuffle(28));
            _discards.Clear();
        }

        public override string GetGameState()
        {
            var sb = new StringBuilder($"State of the board at turn {_turn}:\n")
                .AppendLine($"Turn state is {State}.")
                .AppendLine($"{5 - _liberalTrack.Count()} {_config.Liberal} {_config.Policies} passed.")
                .AppendLine($"{6 - _fascistTrack.Count()} {_config.Fascist} {_config.Policies} passed.")
                .AppendFormat(_config.ThePeopleState + '\n', 3 - _electionTracker)
                .AppendLine($"{_deck.Count} {_config.Policies} in the deck.")
                .AppendLine($"{_discards.Count} {_config.Policies} discarded.")
                .AppendSequence(_confirmedNot, (b, p) => b.AppendLine($"**{p.User.Username}** is Not {_config.Hitler} (Confirmed)."))
                .AppendSequence(Players.Where(p => !p.IsAlive), (b, p) => b.AppendLine($"**{p.User.Username}** is Dead."))
                .Append($"The order of players is: ")
                .AppendLine(String.Join(" -> ", Players.Select(p =>
                {
                    if (!p.IsAlive)
                    {
                        return $"~~{p.User.Username}~~";
                    }
                    else if (p.User.Id == CurrentChancellor?.User.Id || p.User.Id == CurrentPresident.User.Id)
                    {
                        return $"*{p.User.Username}*";
                    }
                    else if (p.User.Id == _lastChancellor?.User.Id || (p.User.Id == _lastPresident?.User.Id && Players.Count > 5))
                    {
                        return $"**{p.User.Username}**";
                    }
                    else
                    {
                        return p.User.Username;
                    }
                })))
                .Append($"(*Italic* = current {_config.President}/{_config.Chancellor}, **bold** = last {_config.President}/{_config.Chancellor}.)");

            return sb.ToString();
        }
    }
}
