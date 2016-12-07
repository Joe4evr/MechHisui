using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;
using Discord.Commands;
using MechHisui.SecretHitler.Collections;
using MechHisui.SecretHitler.Models;

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

        private Stack<PolicyType> _deck;
        private Node<SecretHitlerPlayer> _afterSpecial;
        private SecretHitlerPlayer _lastPresident;
        private SecretHitlerPlayer _chancellorNominee;
        private SecretHitlerPlayer _lastChancellor;
        private SecretHitlerPlayer _specialElected;

        private Stack<PolicyType> _discards = new Stack<PolicyType>();
        private List<SecretHitlerPlayer> _confirmedNot = new List<SecretHitlerPlayer>();
        private int _turn = 0;
        private int _electionTracker = 0;
        private bool _takenVeto = false;

        private IEnumerable<SecretHitlerPlayer> _livingPlayers => Players.Where(p => p.IsAlive);

        internal GameState State { get; private set; }
        internal SecretHitlerPlayer CurrentChancellor { get; private set; }
        internal Node<SecretHitlerPlayer> CurrentPresident
        {
            get { return TurnPlayer; }
            private set { TurnPlayer = value; }
        }
        internal bool VetoUnlocked { get; private set; } = false;

        public SecretHitlerGame(
            IMessageChannel channel,
            IEnumerable<SecretHitlerPlayer> players,
            SecretHitlerConfig config,
            HouseRules houseRules)
            : base(channel, players)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            _config = config;
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
                var others = Players.Where(p =>
                        p.Party == _config.FascistParty
                        && p.User.Id != player.User.Id
                        && p.Role != _config.Hitler);

                var sb = new StringBuilder($"Your party is **{player.Party}**. ")
                    .AppendLine($"Your role is **{player.Role}**.")
                    .AppendWhen(() => player.Role == _config.Hitler, b =>
                        others.Count() == 1
                        ? b.Append($"Your teammate is **{others.Single().User.Username}**. Use this information wisely.")
                        : b.Append("Guard your secret well."))
                    .AppendWhen(() => player.Party == _config.FascistParty, b =>
                    {
                        if (others.Count() == 1)
                        {
                            b.AppendLine($"Your teammate is **{others.Single().User.Username}**.");
                        }
                        else if (others.Count() > 1)
                        {
                            b.AppendLine($"Your teammates are **{String.Join("**, **", others.Select(p => p.User.Username))}**.");
                        }
                        return b.Append($"{_config.Hitler} is **{Players.Single(p => p.Role == _config.Hitler).User.Username}**. Use this information wisely.");
                    });

                await player.SendMessageAsync(sb.ToString());
                await Task.Delay(1000);
            }
            await Channel.SendMessageAsync($"The order of players is: {String.Join(" -> ", Players.Select(p => p.User.Username))}");
            await NextTurn();
        }

        public override async Task NextTurn()
        {
            _turn++;
            State = GameState.StartOfTurn;
            _takenVeto = false;
            if (_specialElected == null)
            {
                do
                {
                    CurrentPresident = CurrentPresident.Next;
                } while (!CurrentPresident.Value.IsAlive);
            }
            else if (_afterSpecial == null)
            {
                _afterSpecial = CurrentPresident.Next;
                CurrentPresident = Players.Find(_specialElected);
            }
            else
            {
                if (_afterSpecial.Value.User.Id != _afterSpecial.Value.User.Id)
                {
                    CurrentPresident = _afterSpecial;
                }
                else
                {
                    CurrentPresident = CurrentPresident.Next;
                }
                _specialElected = null;
                _afterSpecial = null;
            }
            CurrentChancellor = null;
            await Channel.SendMessageAsync($"It is turn {_turn}, the {_config.President} is **{CurrentPresident.Value.User.Username}**. Please choose your {_config.Chancellor}.");
        }

        public async Task NominatedChancellor(IUser nominee)
        {
            if (_turn == 1 && (_houseRules & HouseRules.SkipFirstElection) == HouseRules.SkipFirstElection)
            {
                await Channel.SendMessageAsync($"Because of the applied house rule, this vote will be skipped.");
                return;
            }
            if (nominee.Id == _lastChancellor.User.Id)
            {
                await Channel.SendMessageAsync($"**{nominee.Username}** has been the {_config.Chancellor} last turn and is therefore ineligible.");
                return;
            }
            if (nominee.Id == _lastPresident.User.Id && Players.Count > 5)
            {
                await Channel.SendMessageAsync($"**{nominee.Username}** has been the {_config.President} last turn and is therefore ineligible.");
                return;
            }
            if (Players.Any(p => !p.IsAlive && p.User.Id == nominee.Id))
            {
                await Channel.SendMessageAsync($"**{nominee.Username}** is dead and is therefore ineligible.");
                return;
            }

            _chancellorNominee = Players.Single(p => p.User.Id == nominee.Id);
            State = GameState.VoteForGovernment;
            _votes.Clear();
            _votes.CollectionChangedAsync += votesChanged;
            await Channel.SendMessageAsync($"**{CurrentPresident.Value.User.Username}** has nominated **{nominee.Username}** as their {_config.Chancellor}. PM me `{_config.Yes}` or `{_config.No}` to vote on this proposal.");
            foreach (var player in Players)
            {
                await player.SendMessageAsync($"**{CurrentPresident.Value.User.Username}** nominated **{nominee.Username}** as {_config.Chancellor}. Do you agree?");
                await Task.Delay(1000);
            }
        }

        public async Task ProcessVote(IDMChannel dms, IUser user, string vote)
        {
            if (!_votes.Any(p => p.User.Id == user.Id) &&
                _livingPlayers.Any(p => p.User.Id == user.Id))
            {
                Vote v;
                if (vote.ToLowerInvariant() == _config.Yes.ToLowerInvariant())
                {
                    v = Vote.Yes;
                }
                else if (vote.ToLowerInvariant() == _config.No.ToLowerInvariant())
                {
                    v = Vote.No;
                }
                else
                {
                    await dms.SendMessageAsync("Unacceptable parameter.");
                    return;
                }
                _votes.Add(new PlayerVote(user, v));

                await dms.SendMessageAsync("Your vote has been recorded.");
            }
        }

        public async Task PresidentDiscards(IDMChannel dms, int nr)
        {
            var pick = nr - 1;
            var tmp = _policies[pick];
            await dms.SendMessageAsync($"Removing a {(tmp == PolicyType.Fascist ? _config.Fascist : _config.Liberal)} {_config.Policy}.");
            _discards.Push(tmp);
            _policies.RemoveAt(pick);
            await Channel.SendMessageAsync($"The {_config.President} has discarded one {_config.Policy}.");
            await Task.Delay(1000);
            await ChancellorPick();
        }

        public async Task ChancellorPlays(IDMChannel dms, int nr)
        {
            var pick = nr - 1;
            var tmp = _policies[pick];
            _policies.RemoveAt(pick);
            await dms.SendMessageAsync($"Playing a {(tmp == PolicyType.Fascist ? _config.Fascist : _config.Liberal)} {_config.Policy}.");
            _discards.Push(_policies.Single());
            await Channel.SendMessageAsync($"The {_config.Chancellor} has discarded one {_config.Policy} and played a **{(tmp == PolicyType.Fascist ? _config.Fascist : _config.Liberal)}** {_config.Policy}.");
            await Task.Delay(1000);
            var space = (tmp == PolicyType.Fascist)
                ? _fascistTrack.Dequeue()
                : _liberalTrack.Dequeue();
            await ResolveEffect(space);
            if (_deck.Count < 3)
            {
                ReshuffleDeck();
            }
            _policies.Clear();
        }

        public async Task ChancellorVetos(IDMChannel dms)
        {
            if (!_takenVeto)
            {
                State = GameState.ChancellorVetod;
                await Channel.SendMessageAsync($"The {_config.Chancellor} has opted to veto. Do you consent, Mr./Mrs. {_config.President}?");
                return;
            }
            else
            {
                await dms.SendMessageAsync($"You have already attempted to veto.");
                return;
            }
        }

        public async Task PresidentConsentsVeto(string consent)
        {
            if (!_takenVeto)
            {
                if (consent == "approved")
                {
                    State = GameState.EndOfTurn;
                    _electionTracker++;
                    _takenVeto = true;
                    await Channel.SendMessageAsync($"The {_config.President} has approved the {_config.Chancellor}'s veto.");
                }
                else if (consent == "denied")
                {
                    State = GameState.ChancellorPicks;
                    _takenVeto = true;
                    await Channel.SendMessageAsync($"The {_config.President} has denied veto and the {_config.Chancellor} must play.");
                }
                else
                {
                    await Channel.SendMessageAsync("Unacceptable parameter.");
                }
            }
        }

        public async Task SpecialElection(IUser player)
        {
            if (player != null && Players.Any(p => p.IsAlive &&
                p.User.Id != CurrentChancellor.User.Id &&
                p.User.Id != CurrentPresident.Value.User.Id &&
                p.User.Id == player.Id))
            {
                State = GameState.EndOfTurn;
                _specialElected = Players.Single(p => p.User.Id == player.Id);
                await Channel.SendMessageAsync($"The {_config.President} has Special Elected **{player.Username}**.");
            }
            else
            {
                await Channel.SendMessageAsync("Ineligible for nomination.");
            }
        }

        public async Task KillPlayer(IUser target)
        {
            if (target != null && _livingPlayers.Any(p => p.User.Id == target.Id))
            {
                State = GameState.EndOfTurn;
                var player = _livingPlayers.Single(p => p.User.Id == target.Id);
                player.IsAlive = false;
                await Channel.SendMessageAsync(String.Format(_config.Kill, player.User.Username));
                await Task.Delay(3500);
                if (player.Role == _config.Hitler)
                {
                    await EndGame(String.Format(_config.HitlerWasKilled, _config.Hitler, _config.LiberalParty));
                }
                else
                {
                    _confirmedNot.Add(player);
                    await Channel.SendMessageAsync(String.Format(_config.HitlerNotKilled, player.User.Username, _config.Hitler));
                }
            }
        }

        public async Task InvestigatePlayer(IUser target)
        {
            if (target != null && _livingPlayers.Any(p => p.User.Id == target.Id))
            {
                State = GameState.EndOfTurn;
                await Channel.SendMessageAsync($"The {_config.President} is investigating **{target.Username}**'s loyalty.");
                await Task.Delay(1000);
                var player = _livingPlayers.Single(p => p.User.Id == target.Id);
                await CurrentPresident.Value.SendMessageAsync($"**{player.User.Username}** belongs to the **{player.Party}**. You are not required to answer truthfully.");
            }
        }

        private async Task votesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var l = e.NewItems as IList<PlayerVote>;
                if (l != null && l.Count == Players.Count(p => p.IsAlive))
                {
                    await VotingResults(l);
                }
            }
        }

        private async Task VotingResults(IList<PlayerVote> votes)
        {
            _votes.CollectionChangedAsync -= votesChanged;
            State = GameState.VotingClosed;
            var favored = votes.Count(v => v.Vote == Vote.Yes);
            var opposed = votes.Count(v => v.Vote == Vote.No);
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
                        await Channel.SendMessageAsync(sb.ToString());
                        break;
                    case 2:
                        sb.Append(_config.ThePeopleTwo);
                        await Channel.SendMessageAsync(sb.ToString());
                        break;
                    case 3:
                        sb.AppendLine(_config.ThePeopleThree);
                        await Channel.SendMessageAsync(sb.ToString());
                        await Task.Delay(10000);
                        _electionTracker = 0;
                        var pol = _deck.Pop();
                        await Channel.SendMessageAsync(String.Format(_config.ThePeopleEnacted, pol.ToString()));
                        if (pol == PolicyType.Fascist)
                        {
                            await ResolveEffect(_fascistTrack.Dequeue(), peopleEnacted: true);
                        }
                        else
                        {
                            await ResolveEffect(_liberalTrack.Dequeue(), peopleEnacted: true);
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
                _lastPresident = CurrentPresident.Value;
                CurrentChancellor = _chancellorNominee;
                _lastChancellor = _chancellorNominee;
                _electionTracker = 0;
                if (_fascistTrack.Count() < 3 && !_confirmedNot.Any(u => u.User.Id == CurrentChancellor.User.Id))
                {
                    sb.Append($"The vote has gone through. Now to ask: **Are you {_config.Hitler}**?");
                    await Channel.SendMessageAsync(sb.ToString());
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    var chosen = CurrentChancellor.User;
                    if (Players.Single(p => p.Role == _config.Hitler).User.Id == CurrentChancellor.User.Id)
                    {
                        await Channel.SendMessageAsync($"**{chosen.Username}** is, in fact, {_config.Hitler}.");
                        await EndGame(_config.FascistsWin);
                    }
                    else
                    {
                        _confirmedNot.Add(CurrentChancellor);
                        await Channel.SendMessageAsync($"**{chosen.Username}** is Not {_config.Hitler} (Confirmed). {_config.Parliament} can function.");
                        await DrawPolicies();
                    }
                }
                else
                {
                    sb.Append($"The vote has gone through. {_config.Parliament} can function.");
                    await Channel.SendMessageAsync(sb.ToString());
                    await DrawPolicies();
                }
            }
        }

        private async Task DrawPolicies()
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

            await CurrentPresident.Value.SendMessageAsync(sb.ToString());
        }

        private async Task ChancellorPick()
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
            await CurrentChancellor.SendMessageAsync(sb.ToString());
        }

        private async Task ResolveEffect(BoardSpace space, bool peopleEnacted = false)
        {
            State = GameState.PolicyEnacted;

            if (peopleEnacted)
            {
                switch (space.Type)
                {
                    case BoardSpaceType.ExecutionVeto:
                        VetoUnlocked = true;
                        await Channel.SendMessageAsync($"The {_config.President} and {_config.Chancellor} may veto from now.");
                        return;
                    case BoardSpaceType.FascistWin:
                        await EndGame(_config.FascistsWin);
                        return;
                    case BoardSpaceType.LiberalWin:
                        await EndGame(_config.LiberalsWin);
                        return;
                    default:
                        await Channel.SendMessageAsync($"No action is taken.");
                        return;
                }
            }
            else
            {
                switch (space.Type)
                {
                    case BoardSpaceType.Blank:
                        await Channel.SendMessageAsync($"Nothing happens. Next turn when players are ready.");
                        return;
                    case BoardSpaceType.Examine:
                        await Channel.SendMessageAsync($"The {_config.President} may see the top 3 cards from the {_config.Policy} deck.");
                        var tops = String.Join("**, **", new[]
                        {
                            (_deck.ElementAt(0) == PolicyType.Fascist ? _config.Fascist : _config.Liberal),
                            (_deck.ElementAt(1) == PolicyType.Fascist ? _config.Fascist : _config.Liberal),
                            (_deck.ElementAt(2) == PolicyType.Fascist ? _config.Fascist : _config.Liberal)
                        });
                        await CurrentPresident.Value.SendMessageAsync($"The top 3 {_config.Policy} cards are **{tops}**");
                        return;
                    case BoardSpaceType.Investigate:
                        State = GameState.Investigating;
                        await Channel.SendMessageAsync($"The {_config.President} may investigate one player's Party affinity.");
                        return;
                    case BoardSpaceType.ChooseNextCandidate:
                        State = GameState.SpecialElection;
                        await Channel.SendMessageAsync($"The current {_config.President} may choose the next turn's {_config.President}.");
                        return;
                    case BoardSpaceType.Execution:
                        State = GameState.Kill;
                        await Channel.SendMessageAsync($"The {_config.President} may choose another player to kill.");
                        return;
                    case BoardSpaceType.ExecutionVeto:
                        VetoUnlocked = true;
                        State = GameState.Kill;
                        await Channel.SendMessageAsync($"The {_config.President} may choose another player to kill. Also, the {_config.President} and {_config.Chancellor} may veto.");
                        return;
                    case BoardSpaceType.FascistWin:
                        await EndGame(_config.FascistsWin);
                        return;
                    case BoardSpaceType.LiberalWin:
                        await EndGame(_config.LiberalsWin);
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
            _discards = new Stack<PolicyType>();
        }

        public override string GetGameState()
        {
            var sb = new StringBuilder($"State of the board at turn {_turn}:\n")
                .AppendLine($"Turn state is {State.ToString()}.")
                .AppendLine($"{5 - _liberalTrack.Count()} {_config.Liberal} {_config.Policies} passed.")
                .AppendLine($"{6 - _fascistTrack.Count()} {_config.Fascist} {_config.Policies} passed.")
                .AppendFormat(_config.ThePeopleState + '\n', (3 - _electionTracker))
                .AppendLine($"{_deck.Count} {_config.Policies} in the deck.")
                .AppendLine($"{_discards.Count} {_config.Policies} discarded.")
                .AppendSequence(_confirmedNot, (b, player) => b.AppendLine($"**{player.User.Username}** is Not {_config.Hitler} (Confirmed)."))
                .AppendSequence(Players.Where(p => !p.IsAlive), (b, player) => b.AppendLine($"**{player.User.Username}** is Dead."))
                .Append($"The order of players is: ")
                .AppendLine(String.Join(" -> ", Players.Select(player =>
                {
                    if (!player.IsAlive)
                    {
                        return $"~~{player.User.Username}~~";
                    }
                    else if (player.User.Id == CurrentChancellor.User.Id || player.User.Id == CurrentPresident.Value.User.Id)
                    {
                        return $"*{player.User.Username}*";
                    }
                    else if (player.User.Id == _lastChancellor.User.Id || (player.User.Id == _lastPresident.User.Id && Players.Count > 5))
                    {
                        return $"**{player.User.Username}**";
                    }
                    else
                    {
                        return player.User.Username;
                    }
                })))
                .Append($"(*Italic* = current {_config.President}/{_config.Chancellor}, **bold** = last {_config.President}/{_config.Chancellor}.)");

            return sb.ToString();
        }
    }
}
