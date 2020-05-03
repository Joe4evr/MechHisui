using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;
using Discord.Addons.MpGame.Collections;
using SharedExtensions;
using SharedExtensions.Collections;
using MechHisui.SecretHitler.Models;

namespace MechHisui.SecretHitler
{
    public sealed class SecretHitlerGame : GameBase<SecretHitlerPlayer>
    {
        private readonly ISecretHitlerTheme _theme;
        private readonly HouseRules _houseRules;

        private readonly Queue<BoardSpaceType> _liberalTrack = new Queue<BoardSpaceType>(5);
        private readonly Queue<BoardSpaceType> _fascistTrack = new Queue<BoardSpaceType>(6);
        private readonly AsyncObservableCollection<PlayerVote> _votes = new AsyncObservableCollection<PlayerVote>();

        private readonly PolicyDeck _deck = new PolicyDeck(
            Enumerable.Repeat(PolicyType.Fascist, 11)
                .Concat(Enumerable.Repeat(PolicyType.Liberal, 6))
                .Select(p => new PolicyCard(p))
                .Shuffle(32));
        private readonly PolicyDiscard _discards = new PolicyDiscard();
        private readonly Hand<PolicyCard> _drawnPolicies = new Hand<PolicyCard>();

        private int _turn = 0;
        private int _electionTracker = 0;
        private bool _takenVeto = false;

        private Node<SecretHitlerPlayer>? _afterSpecial;
        private SecretHitlerPlayer? _chancellorNominee;
        private SecretHitlerPlayer? _lastChancellor;
        private SecretHitlerPlayer? _lastPresident;
        private SecretHitlerPlayer? _specialElected;

        internal IEnumerable<SecretHitlerPlayer> LivingPlayers => Players.Where(p => p.IsAlive);

        internal SecretHitlerPlayer CurrentPresident => TurnPlayer.Value;
        internal SecretHitlerPlayer? CurrentChancellor { get; private set; }
        internal bool VetoUnlocked { get; private set; } = false;
        internal GameState State { get; private set; } = GameState.Setup;

        internal SecretHitlerGame(
            IMessageChannel channel, IEnumerable<SecretHitlerPlayer> players,
            ISecretHitlerTheme theme, HouseRules houseRules)
            : base(channel, players, setFirstPlayerImmediately: true)
        {
            _theme = theme ?? throw new ArgumentNullException(nameof(theme));
            _houseRules = houseRules;

            _liberalTrack.Enqueue(BoardSpaceType.Blank);
            _liberalTrack.Enqueue(BoardSpaceType.Blank);
            _liberalTrack.Enqueue(BoardSpaceType.Blank);
            _liberalTrack.Enqueue(BoardSpaceType.Blank);
            _liberalTrack.Enqueue(BoardSpaceType.LiberalWin);

            switch (Players.Count)
            {
                case 5:
                case 6:
                    _fascistTrack.Enqueue(BoardSpaceType.Blank);
                    _fascistTrack.Enqueue(BoardSpaceType.Blank);
                    _fascistTrack.Enqueue(BoardSpaceType.Examine);
                    break;
                case 7:
                case 8:
                    _fascistTrack.Enqueue(BoardSpaceType.Blank);
                    _fascistTrack.Enqueue(BoardSpaceType.Investigate);
                    _fascistTrack.Enqueue(BoardSpaceType.ChooseNextCandidate);
                    break;
                case 9:
                case 10:
                    _fascistTrack.Enqueue(BoardSpaceType.Investigate);
                    _fascistTrack.Enqueue(BoardSpaceType.Investigate);
                    _fascistTrack.Enqueue(BoardSpaceType.ChooseNextCandidate);
                    break;
                default:
                    throw new InvalidOperationException("Player count should be between 5 and 10.");
            }
            _fascistTrack.Enqueue(BoardSpaceType.Execution);
            _fascistTrack.Enqueue(BoardSpaceType.ExecutionVeto);
            _fascistTrack.Enqueue(BoardSpaceType.FascistWin);
        }

        public override Task SetupGame()
            => Task.CompletedTask;

        public override async Task StartGame()
        {
            foreach (var player in Players)
            {
                var otherFacists = Players.Where(p =>
                        p.Party == _theme.FascistParty
                        && p.User.Id != player.User.Id
                        && p.Role != _theme.Hitler).ToList();

                var sb = new StringBuilder($"Your party is **{player.Party}**. ")
                    .AppendLine($"Your role is **{player.Role}**.")
                    .AppendWhen(player.Role == _theme.Hitler, b =>
                        otherFacists.Count == 1
                            ? b.Append($"Your teammate is **{otherFacists.Single().User.Username}**. Use this information wisely.")
                            : b.Append("Guard your secret well."))
                    .AppendWhen(player.Party == _theme.FascistParty && player.Role != _theme.Hitler, b =>
                    {
                        if (otherFacists.Count == 1)
                        {
                            return b.Append($"{_theme.Hitler} is **{otherFacists.Single().User.Username}**. Use this information wisely.");
                        }
                        else
                        {
                            b.AppendLine($"Your teammates are **{String.Join("**, **", otherFacists.Select(p => p.User.Username))}**.");
                            return b.Append($"{_theme.Hitler} is **{Players.Single(p => p.Role == _theme.Hitler).User.Username}**. Use this information wisely.");
                        }
                    });

                await player.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
                await Task.Delay(1000).ConfigureAwait(false);
            }
            await Channel.SendMessageAsync($"The order of players is: {String.Join(" -> ", Players.Select(p => p.User.Username))}").ConfigureAwait(false);
            await NextTurn().ConfigureAwait(false);
        }

        public override Task NextTurn()
        {
            _turn++;
            State = GameState.StartOfTurn;
            _takenVeto = false;
            _lastChancellor = CurrentChancellor;
            _chancellorNominee = null;
            CurrentChancellor = null;

            if (_turn > 1)
            {
                _lastPresident = CurrentPresident;
                if (_specialElected != null)
                {
                    TurnPlayer = Players.Find(_specialElected)!;
                    _specialElected = null;
                }
                else if (_afterSpecial != null)
                {
                    TurnPlayer = _afterSpecial;
                    _afterSpecial = null;
                }
                else
                {
                    do TurnPlayer = TurnPlayer.Next;
                    while (!CurrentPresident.IsAlive);
                }
            }

            return Channel.SendMessageAsync($"It is turn {_turn}, the {_theme.President} is **{CurrentPresident.User.Username}**. Please choose your {_theme.Chancellor}.");
        }

        public async Task NominatedChancellor(SecretHitlerPlayer nominee)
        {
            if (nominee.User.Id == CurrentPresident.User.Id)
            {
                await Channel.SendMessageAsync($"**{nominee.User.Username}** is the current {_theme.President} and is therefore ineligible.").ConfigureAwait(false);
                return;
            }
            if (nominee.User.Id == _lastChancellor?.User.Id)
            {
                await Channel.SendMessageAsync($"**{nominee.User.Username}** has been the {_theme.Chancellor} last turn and is therefore ineligible.").ConfigureAwait(false);
                return;
            }
            if (nominee.User.Id == _lastPresident?.User.Id && Players.Count > 5)
            {
                await Channel.SendMessageAsync($"**{nominee.User.Username}** has been the {_theme.President} last turn and is therefore ineligible.").ConfigureAwait(false);
                return;
            }
            if (!nominee.IsAlive)
            {
                await Channel.SendMessageAsync($"**{nominee.User.Username}** is dead and is therefore ineligible.").ConfigureAwait(false);
                return;
            }
            if (_turn == 1 && (_houseRules & HouseRules.SkipFirstElection) == HouseRules.SkipFirstElection)
            {
                await Channel.SendMessageAsync($"Because of the applied house rule, this vote will be skipped.").ConfigureAwait(false);
                CurrentChancellor = nominee;
                await DrawPolicies().ConfigureAwait(false);
                return;
            }

            _chancellorNominee = nominee;
            State = GameState.VoteForGovernment;
            _votes.Clear();
            _votes.CollectionChangedAsync += VotesChanged;
            await Channel.SendMessageAsync($"**{CurrentPresident.User.Username}** has nominated **{nominee.User.Username}** as their {_theme.Chancellor}. PM me `{_theme.Yes}` or `{_theme.No}` to vote on this proposal.").ConfigureAwait(false);
            foreach (var player in LivingPlayers)
            {
                await player.SendMessageAsync($"**{CurrentPresident.User.Username}** nominated **{nominee.User.Username}** as {_theme.Chancellor}. Do you agree?").ConfigureAwait(false);
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }

        public async Task ProcessVote(IDMChannel dms, IUser user, string vote)
        {
            if (!_votes.Any(p => p.User.Id == user.Id)
                && LivingPlayers.Any(p => p.User.Id == user.Id))
            {
                Vote v;
                if (vote.Equals(_theme.Yes, StringComparison.OrdinalIgnoreCase))
                {
                    v = Vote.Yes;
                }
                else if (vote.Equals(_theme.No, StringComparison.OrdinalIgnoreCase))
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

        private async Task VotesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add
                && e.NewItems is IList<PlayerVote> l
                && l.Count == LivingPlayers.Count())
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
                    b.AppendLine($"**{vote.User.Username}**: {(vote.Vote == Vote.Yes ? _theme.Yes : _theme.No)}"))
                .AppendLine($"Total in favor: {favored} - Total opposed: {opposed}");

            if (opposed >= favored)
            {
                sb.Append($"The vote has **not** gone through. {_theme.Parliament} is stalled and ");
                await HungParliament(sb).ConfigureAwait(false);
            }
            else
            {
                CurrentChancellor = _chancellorNominee!;
                if (_fascistTrack.Count <= 3 && !CurrentChancellor.IsConfirmedNotHitler)
                {
                    sb.Append($"The vote has gone through. Now to ask: **Are you {_theme.Hitler}**?");
                    await Channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                    var chosen = CurrentChancellor.User;
                    if (Players.Single(p => p.Role == _theme.Hitler).User.Id == chosen.Id)
                    {
                        await Channel.SendMessageAsync($"**{chosen.Username}** is, in fact, {_theme.Hitler}.").ConfigureAwait(false);
                        await EndGame(_theme.FascistsWin).ConfigureAwait(false);
                    }
                    else
                    {
                        CurrentChancellor.ConfirmedNotHitler();
                        await Channel.SendMessageAsync($"**{chosen.Username}** is Not {_theme.Hitler} (Confirmed). {_theme.Parliament} can function.").ConfigureAwait(false);
                        await DrawPolicies().ConfigureAwait(false);
                    }
                }
                else
                {
                    sb.Append($"The vote has gone through. {_theme.Parliament} can function.");
                    await Channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
                    await DrawPolicies().ConfigureAwait(false);
                }
            }
        }

        private async Task HungParliament(StringBuilder sb)
        {
            _electionTracker++;
            switch (_electionTracker)
            {
                case 1:
                    sb.Append(_theme.FirstStall);
                    await Channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
                    break;
                case 2:
                    sb.Append(_theme.SecondStall);
                    await Channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
                    break;
                case 3:
                    sb.AppendLine(_theme.ThirdStall);
                    await Channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
                    await Task.Delay(5000).ConfigureAwait(false);
                    _electionTracker = 0;
                    _lastChancellor = null;
                    _lastPresident = null;
                    var pol = _deck.Draw();
                    await Channel.SendMessageAsync(_theme.ThePeopleEnacted(pol.ToString()!)).ConfigureAwait(false);
                    await ResolveEffect(((pol.PolicyType == PolicyType.Fascist) ? _fascistTrack : _liberalTrack).Dequeue(), peopleEnacted: true).ConfigureAwait(false);
                    if (_deck.Count < 3)
                    {
                        await ReshuffleDeck().ConfigureAwait(false);
                    }
                    break;
            }
        }

        private Task DrawPolicies()
        {
            State = GameState.PresidentPicks;

            _drawnPolicies.Clear();
            foreach (var policy in new[] { _deck.Draw(), _deck.Draw(), _deck.Draw() })
            {
                _drawnPolicies.Add(policy);
            }

            var sb = new StringBuilder($"You have drawn the following {_theme.Policies}:\n");
            var counter = 0;
            foreach (var policy in _drawnPolicies.Browse())
            {
                sb.AppendLine($"\t**{++counter}**: {(policy.PolicyType == PolicyType.Fascist ? _theme.Fascist : _theme.Liberal)}");
            }

            sb.Append($"Which {_theme.Policy} will you discard? The other two are automatically sent to your {_theme.Chancellor}.");

            return CurrentPresident.SendMessageAsync(sb.ToString());
        }

        public async Task PresidentDiscards(IDMChannel dms, int nr)
        {
            var tmp = _drawnPolicies.TakeAt(nr - 1);
            await dms.SendMessageAsync($"Removing a {(tmp.PolicyType == PolicyType.Fascist ? _theme.Fascist : _theme.Liberal)} {_theme.Policy}.").ConfigureAwait(false);

            _discards.Put(tmp);
            await Channel.SendMessageAsync($"The {_theme.President} has discarded one {_theme.Policy}.").ConfigureAwait(false);

            await Task.Delay(1000).ConfigureAwait(false);
            await ChancellorPick().ConfigureAwait(false);
        }

        private Task ChancellorPick()
        {
            State = GameState.ChancellorPicks;
            var sb = new StringBuilder($"The {_theme.President} has given you these {_theme.Policies}:");
            var counter = 0;
            foreach (var policy in _drawnPolicies.Browse())
            {
                sb.AppendLine($"\t**{++counter}**: {(policy.PolicyType == PolicyType.Fascist ? _theme.Fascist : _theme.Liberal)}");
            }

            if (!VetoUnlocked)
            {
                sb.Append($"Which {_theme.Policy} will you play?");
            }
            else
            {
                sb.Append($"Will you play one or `veto`?");
            }
            return CurrentChancellor!.SendMessageAsync(sb.ToString());
        }

        public async Task ChancellorPlays(IDMChannel dms, int nr)
        {
            var tmp = _drawnPolicies.TakeAt(nr - 1);
            await dms.SendMessageAsync($"Playing a {(tmp.PolicyType== PolicyType.Fascist ? _theme.Fascist : _theme.Liberal)} {_theme.Policy}.").ConfigureAwait(false);

            foreach (var discard in _drawnPolicies.Clear())
            {
                _discards.Put(discard);
            }
            await Channel.SendMessageAsync($"The {_theme.Chancellor} has discarded one {_theme.Policy} and played a **{(tmp.PolicyType == PolicyType.Fascist ? _theme.Fascist : _theme.Liberal)}** {_theme.Policy}.").ConfigureAwait(false);

            _electionTracker = 0;
            if (_deck.Count < 3)
            {
                await ReshuffleDeck().ConfigureAwait(false);
            }
            await Task.Delay(1000).ConfigureAwait(false);

            var space = (tmp.PolicyType == PolicyType.Fascist)
                ? _fascistTrack.Dequeue()
                : _liberalTrack.Dequeue();
            await ResolveEffect(space).ConfigureAwait(false);
        }

        private async Task ResolveEffect(BoardSpaceType space, bool peopleEnacted = false)
        {
            State = GameState.PolicyEnacted;

            if (peopleEnacted)
            {
                switch (space)
                {
                    case BoardSpaceType.ExecutionVeto:
                        VetoUnlocked = true;
                        await Channel.SendMessageAsync($"The {_theme.President} and {_theme.Chancellor} may veto from now.").ConfigureAwait(false);
                        return;
                    case BoardSpaceType.FascistWin:
                        await EndGame(_theme.FascistsWin).ConfigureAwait(false);
                        return;
                    case BoardSpaceType.LiberalWin:
                        await EndGame(_theme.LiberalsWin).ConfigureAwait(false);
                        return;
                    default:
                        await Channel.SendMessageAsync($"No action is taken.").ConfigureAwait(false);
                        return;
                }
            }
            else
            {
                switch (space)
                {
                    case BoardSpaceType.Blank:
                        await Channel.SendMessageAsync($"Nothing happens. Next turn when players are ready.").ConfigureAwait(false);
                        return;
                    case BoardSpaceType.Examine:
                        await Channel.SendMessageAsync($"The {_theme.President} may see the top 3 cards from the {_theme.Policy} deck.").ConfigureAwait(false);
                        string tops = String.Join("**, **", _deck.PeekTop(3)
                            .Select(p => p.PolicyType == PolicyType.Fascist ? _theme.Fascist : _theme.Liberal));
                        await CurrentPresident.SendMessageAsync($"The top 3 {_theme.Policy} cards are **{tops}**").ConfigureAwait(false);
                        return;
                    case BoardSpaceType.Investigate:
                        State = GameState.Investigating;
                        await Channel.SendMessageAsync($"The {_theme.President} may investigate 1 (one) player's Party affinity.").ConfigureAwait(false);
                        return;
                    case BoardSpaceType.ChooseNextCandidate:
                        State = GameState.SpecialElection;
                        await Channel.SendMessageAsync($"The current {_theme.President} may choose the next turn's {_theme.President}.").ConfigureAwait(false);
                        return;
                    case BoardSpaceType.Execution:
                        State = GameState.Kill;
                        await Channel.SendMessageAsync($"The {_theme.President} may choose another player to kill.").ConfigureAwait(false);
                        return;
                    case BoardSpaceType.ExecutionVeto:
                        VetoUnlocked = true;
                        State = GameState.Kill;
                        await Channel.SendMessageAsync($"The {_theme.President} may choose another player to kill. Also, the {_theme.President} and {_theme.Chancellor} may veto.").ConfigureAwait(false);
                        return;
                    case BoardSpaceType.FascistWin:
                        await EndGame(_theme.FascistsWin).ConfigureAwait(false);
                        return;
                    case BoardSpaceType.LiberalWin:
                        await EndGame(_theme.LiberalsWin).ConfigureAwait(false);
                        return;
                }
            }
        }

        public async Task InvestigatePlayer(SecretHitlerPlayer target)
        {
            if (target != null)
            {
                if (target.IsInvestigated)
                {
                    await Channel.SendMessageAsync($"Player **{target.User.Username}** has already been investigated this game.").ConfigureAwait(false);
                    return;
                }

                await Channel.SendMessageAsync($"The {_theme.President} is investigating **{target.User.Username}**'s loyalty.").ConfigureAwait(false);
                await Task.Delay(1000).ConfigureAwait(false);
                target.Investigated();
                await CurrentPresident.SendMessageAsync($"**{target.User.Username}** belongs to the **{target.Party}**. You are not required to be truthful.").ConfigureAwait(false);
                State = GameState.EndOfTurn;
            }
        }

        public Task SpecialElection(SecretHitlerPlayer target)
        {
            if (target != null && target.User.Id != CurrentPresident.User.Id)
            {
                State = GameState.EndOfTurn;
                _specialElected = target;
                _afterSpecial = TurnPlayer.Next;
                return Channel.SendMessageAsync($"The {_theme.President} has Special Elected **{target.User.Username}**.");
            }
            else
            {
                return Channel.SendMessageAsync("Ineligible for nomination.");
            }
        }

        public async Task KillPlayer(SecretHitlerPlayer target)
        {
            if (target != null)
            {
                State = GameState.EndOfTurn;
                target.Killed();
                await Channel.SendMessageAsync(_theme.Kill(target.User.Username)).ConfigureAwait(false);
                await Task.Delay(3500).ConfigureAwait(false);
                if (target.Role == _theme.Hitler)
                {
                    await EndGame(_theme.HitlerWasKilled()).ConfigureAwait(false);
                }
                else
                {
                    target.ConfirmedNotHitler();
                    await Channel.SendMessageAsync(_theme.HitlerNotKilled(target.User.Username)).ConfigureAwait(false);
                }
            }
        }

        public Task ChancellorVetos(IDMChannel dms)
        {
            if (!_takenVeto)
            {
                State = GameState.ChancellorVetod;
                return Channel.SendMessageAsync($"The {_theme.Chancellor} has opted to veto. Do you consent, Mr./Mrs. {_theme.President}?");
            }
            else
            {
                return dms.SendMessageAsync($"You have already attempted to veto.");
            }
        }

        public async Task PresidentConsentsVeto(string consent)
        {
            if (!_takenVeto)
            {
                if (consent.Equals("approved", StringComparison.OrdinalIgnoreCase))
                {
                    await Channel.SendMessageAsync($"The {_theme.President} has approved the {_theme.Chancellor}'s veto. Next turn when players are ready.").ConfigureAwait(false);
                    foreach (var p in _drawnPolicies.Clear())
                    {
                        _discards.Put(p);
                    }
                    _drawnPolicies.Clear();
                    _electionTracker++;
                    _takenVeto = true;
                    State = GameState.EndOfTurn;
                }
                else if (consent.Equals("denied", StringComparison.OrdinalIgnoreCase))
                {
                    await Channel.SendMessageAsync($"The {_theme.President} has denied veto and the {_theme.Chancellor} must play.").ConfigureAwait(false);
                    State = GameState.ChancellorPicks;
                    _takenVeto = true;
                }
                else
                {
                    await Channel.SendMessageAsync("Unacceptable parameter.").ConfigureAwait(false);
                }
            }
        }

        protected override Task OnGameEnd()
        {
            var sb = new StringBuilder("The game is over.\n")
                .AppendLine($"The {_theme.Fascist}s were **{String.Join("**, **", Players.Where(p => p.Party == _theme.FascistParty).Select(p => p.User.Username))}**.")
                .AppendLine($"The {_theme.Liberal}s were **{String.Join("**, **", Players.Where(p => p.Party == _theme.LiberalParty).Select(p => p.User.Username))}**.");
            return Channel.SendMessageAsync(sb.ToString());
        }

        private Task ReshuffleDeck()
        {
            _deck.Shuffle(cards => cards.Concat(_discards.Clear()).Shuffle(28));

            return Channel.SendMessageAsync($"The {_theme.Policy} Deck has been reshuffled.");
        }

        public override string GetGameState()
        {
            var sb = new StringBuilder($"State of the board at turn {_turn}:\n")
                .AppendLine($"Turn state is {State}.")
                .AppendLine($"{5 - _liberalTrack.Count}/5 {_theme.Liberal} {_theme.Policies} passed.")
                .AppendLine($"{6 - _fascistTrack.Count}/6 {_theme.Fascist} {_theme.Policies} passed.")
                .AppendLine(_theme.ThePeopleState(3 - _electionTracker))
                .AppendLine($"{_deck.Count} {_theme.Policies} in the deck.")
                .AppendLine($"{_discards.Count} {_theme.Policies} discarded.")
                .AppendSequence(Players.Where(p => p.IsConfirmedNotHitler), (b, p) => b.AppendLine($"**{p.User.Username}** is Not {_theme.Hitler} (Confirmed)."))
                .AppendSequence(Players.Where(p => !p.IsAlive), (b, p) => b.AppendLine($"**{p.User.Username}** is Dead."))
                .Append($"The order of players is: ")
                .AppendLine(String.Join(" -> ", Players.Select(p =>
                {
                    var n = p.User.Username;
                    if (!p.IsAlive)
                    {
                        n = $"~~{n}~~";
                    }
                    if (p.User.Id == CurrentChancellor?.User.Id || p.User.Id == CurrentPresident.User.Id)
                    {
                        n = $"*{n}*";
                    }
                    if (p.User.Id == _lastChancellor?.User.Id || (p.User.Id == _lastPresident?.User.Id && Players.Count > 5))
                    {
                        n = $"**{n}**";
                    }
                    return n;
                })))
                .Append($"(*Italic* = current {_theme.President}/{_theme.Chancellor}, **bold** = last {_theme.President}/{_theme.Chancellor}.)");

            return sb.ToString();
        }

        public override Embed GetGameStateEmbed()
        {
            return new EmbedBuilder
            {
                Title = $"State of the board at turn {_turn}:",
                Description = $@"Turn state is {State}.
{5 - _liberalTrack.Count}/5 {_theme.Liberal} {_theme.Policies} passed.
{6 - _fascistTrack.Count}/6 {_theme.Fascist} {_theme.Policies} passed.
{_theme.ThePeopleState(3 - _electionTracker)}
{_deck.Count} {_theme.Policies} in the deck.
{_discards.Count} {_theme.Policies} discarded.",
            }.AddFieldSequence(Players, (fb, p) =>
            {
                fb.IsInline = true;
                fb.Name = p.User.Username;

                var sb = new StringBuilder()
                    .AppendWhen(p.User.Id == CurrentPresident.User.Id,   b => b.AppendLine($"Current {_theme.President}"))
                    .AppendWhen(p.User.Id == CurrentChancellor?.User.Id, b => b.AppendLine($"Current {_theme.Chancellor}"))
                    .AppendWhen(p.User.Id == _lastPresident?.User.Id,    b => b.AppendLine($"Last {_theme.President}"))
                    .AppendWhen(p.User.Id == _lastChancellor?.User.Id,   b => b.AppendLine($"Last {_theme.Chancellor}"))
                    .AppendWhen(p.IsConfirmedNotHitler, b => b.AppendLine($"Not {_theme.Hitler} (Confirmed)"))
                    .AppendWhen(!p.IsAlive, b => b.AppendLine("Is dead"));

                fb.Value = (sb.Length > 0)
                    ? sb.ToString()
                    : "Just some schmuck";
            }).Build();
        }
    }
}
