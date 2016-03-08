using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using JiiLib;

namespace MechHisui.SecretHitler
{
    public class SecretHitler
    {
        public IList<BoardSpace> LiberalTrack { get; }
        public IList<BoardSpace> FascistTrack { get; }
        public Stack<PolicyType> Deck { get; private set; }
        public Stack<PolicyType> Discards { get; private set; } = new Stack<PolicyType>();

        private readonly SecretHitlerConfig _config;
        private readonly Channel _channel;
        private readonly IList<Player> _players;

        private ulong _currentPresident;
        private ulong _lastPresident;
        private ulong _chancellorNominee;
        private ulong _currentChancellor;
        private ulong _lastChancellor;
        private int _electionTracker = 0;
        private IList<PolicyType> _policies;
        private bool _vetoUnlocked = false;
        private int _turn = 0;
        private List<User> _confirmedNot = new List<User>();
        private GameState _state;
        private List<PlayerVote> _votes;
        private bool _takenVeto = false;
        private ulong _specialElected = 0;
        private int _deadCounter = 0;
        private HouseRules _houseRules;

        private event PropertyChangedEventHandler PropertyChanged;

        public SecretHitler(SecretHitlerConfig config, Channel mainChannel, IList<User> users, HouseRules house = HouseRules.None)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (mainChannel == null) throw new ArgumentNullException(nameof(mainChannel));
            if (users == null) throw new ArgumentNullException(nameof(users));

            _state = GameState.Setup;
            _config = config;
            _channel = mainChannel;
            _players = new List<Player>();
            _houseRules = house;
            int fas = 0;
            switch (users.Count)
            {
                case 5:
                case 6:
                    fas = 2;
                    break;
                case 7:
                case 8:
                    fas = 3;
                    break;
                case 9:
                case 10:
                    fas = 4;
                    break;
                default:
                    break;
            }
            for (int i = 0; i < 16; i++)
            {
                users = (List<User>)users.Shuffle();
            }

            foreach (var user in users)
            {
                if (_players.Count == 0)
                {
                    _players.Add(new Player(user, _config.FascistParty, _config.Hitler));
                }
                else if (_players.Count(p => p.Party == _config.FascistParty) < fas)
                {
                    _players.Add(new Player(user, _config.FascistParty, _config.Fascist));
                }
                else
                {
                    _players.Add(new Player(user, _config.LiberalParty, _config.Liberal));
                }
            }

            for (int i = 0; i < 32; i++)
            {
                _players = (List<Player>)_players.Shuffle();
            }
            //_allPlayers = _players.Select(p => p.User.Id).ToArray();
            LiberalTrack = new List<BoardSpace>
            {
                new BoardSpace(BoardSpaceType.Blank),
                new BoardSpace(BoardSpaceType.Blank),
                new BoardSpace(BoardSpaceType.Blank),
                new BoardSpace(BoardSpaceType.Blank),
                new BoardSpace(BoardSpaceType.LiberalWin)
            };
            FascistTrack = new List<BoardSpace>();

            switch (_players.Count)
            {
                case 5:
                case 6:
                    FascistTrack.Add(new BoardSpace(BoardSpaceType.Blank));
                    FascistTrack.Add(new BoardSpace(BoardSpaceType.Blank));
                    FascistTrack.Add(new BoardSpace(BoardSpaceType.Examine));
                    break;
                case 7:
                case 8:
                    FascistTrack.Add(new BoardSpace(BoardSpaceType.Blank));
                    FascistTrack.Add(new BoardSpace(BoardSpaceType.Investigate));
                    FascistTrack.Add(new BoardSpace(BoardSpaceType.ChooseNextCandidate));
                    break;
                case 9:
                case 10:
                    FascistTrack.Add(new BoardSpace(BoardSpaceType.Investigate));
                    FascistTrack.Add(new BoardSpace(BoardSpaceType.Investigate));
                    FascistTrack.Add(new BoardSpace(BoardSpaceType.ChooseNextCandidate));
                    break;
                default:
                    break;
            }
            FascistTrack.Add(new BoardSpace(BoardSpaceType.Execution));
            FascistTrack.Add(new BoardSpace(BoardSpaceType.ExecutionVeto));
            FascistTrack.Add(new BoardSpace(BoardSpaceType.FascistWin));

            Deck = new Stack<PolicyType>(
                Enumerable.Repeat(PolicyType.Fascist, 11)
                .Concat(Enumerable.Repeat(PolicyType.Liberal, 6)));

            for (int i = 0; i < 32; i++)
            {
                Deck = new Stack<PolicyType>(Deck.Shuffle());
            }

            _channel.Client.MessageReceived += ProcessMessage;
            PropertyChanged += PropChanged;


            //_timer = new Timer(cb =>
            //{

            //},
            //null,
            //Timeout.Infinite,
            //10000);
        }

        public async Task SetupGame()
        {
            foreach (var player in _players)
            {
                var others = _players.Where(p =>
                        p.Party == _config.FascistParty
                        && p.Role != _config.Hitler);
                await player.User.CreatePMChannel();
                var sb = new StringBuilder($"Your party is **{player.Party}**. ")
                    .AppendLine($"Your role is **{player.Role}**.");
                if (player.Role == _config.Hitler)
                {
                    if (others.Count() == 1)
                    {
                        sb.Append($"Your teammate is **{others.Single().User.Name}**. Use this information wisely.");
                    }
                    else
                    {
                        sb.Append("Guard your secret well.");
                    }
                }
                else if (player.Party == _config.FascistParty)
                {
                    if (others.Count() == 1)
                    {
                        sb.AppendLine($"Your teammate is **{others.Single().User.Name}**.");
                    }
                    else if (others.Count() > 1)
                    {
                        sb.AppendLine($"Your teammates are **{String.Join("**, **", others.Where(p => p.User.Id != player.User.Id).Select(p => p.User.Name))}**.");
                    }
                    sb.Append($"{_config.Hitler} is **{_players.Single(p => p.Role == _config.Hitler).User.Name}**. Use this information wisely.");
                }

                await player.User.PrivateChannel.SendMessage(sb.ToString());
                await Task.Delay(1000);
            }
            await _channel.SendMessage($"The order of players is: {String.Join(" -> ", _players.Select(p => p.User.Name))}");
        }

        public async Task StartTurn()
        {
            _state = GameState.StartOfTurn;
            _takenVeto = false;
            if (_specialElected <= 1)
            {
                //Console.WriteLine($"Turn: {_turn} - SE: {_specialElected} - Dead counter: {_deadCounter} - Players: {_players.Count} - i: {i}");
                do
                {
                    int i = ((_turn - (int)_specialElected) + _deadCounter) % _players.Count();
                    if (!_players.ElementAt(i).IsAlive)
                    {
                        _deadCounter++;
                    }
                    _currentPresident = _players.ElementAt(i).User.Id;
                } while (_players.Where(p => !p.IsAlive).Select(p => p.User.Id).Contains(_currentPresident));
            }
            else
            {
                _currentPresident = _specialElected;
                _specialElected = 1;
            }
            _currentChancellor = 0;
            _turn++;
            await _channel.SendMessage($"It is turn {_turn}, the {_config.President} is **{_players.Single(p => p.User.Id == _currentPresident).User.Name}**. Please choose your {_config.Chancellor}.");
        }

        //internal async Task TestTurn(User testUser)
        //{
        //    _state = GameState.StartOfTurn;
        //    _currentPresident = testUser.Id;
        //    _turn++;
        //    await _channel.SendMessage($"It is turn {_turn}, the {_config.President} is **{testUser.Name}**. Please choose your {_config.Chancellor}.");
        //}

        public async Task NominatedChancellor(User nominee)
        {
            if (_turn == 1 && (_houseRules & HouseRules.SkipFirstElection) == HouseRules.SkipFirstElection)
            {
                await _channel.SendMessage($"Because of the applied house rule, this vote will be skipped.");
                return;
            }
            if (nominee.Id == _lastChancellor)
            {
                await _channel.SendMessage($"**{nominee.Name}** has has been the {_config.Chancellor} last time and is therefore ineligable.");
                return;
            }
            if (nominee.Id == _lastPresident && _players.Count > 5)
            {
                await _channel.SendMessage($"**{nominee.Name}** has has been the {_config.President} last time and is therefore ineligable.");
                return;
            }

            if (_players.Where(p => !p.IsAlive).Select(p => p.User.Id).Contains(nominee.Id))
            {
                await _channel.SendMessage($"**{nominee.Name}** is dead and is therefore ineligable.");
                return;
            }

            _chancellorNominee = nominee.Id;
            _state = GameState.VoteForGovernment;
            _votes = new List<PlayerVote>();
            await _channel.SendMessage($"**{_channel.GetUser(_currentPresident).Name}** has nominated **{nominee.Name}** as their {_config.Chancellor}. PM me `{_config.Yes}` or `{_config.No}` to vote on this proposal.");
            foreach (var player in _players)
            {
                await player.User.PrivateChannel.SendMessage($"**{_channel.GetUser(_currentPresident).Name}** nominated **{nominee.Name}** as {_config.Chancellor}.");
                await Task.Delay(1000);
            }
        }

        //internal async Task TestNomination(User nominee, User pres)
        //{
        //    _chancellorNominee = nominee.Id;
        //    _state = GameState.VoteForGovernment;
        //    _votes = new List<PlayerVote>();
        //    await _channel.SendMessage($"**{pres.Name}** has nominated {nominee.Name} as their {_config.Chancellor}. PM me `{_config.Yes}` or `{_config.No}` to vote on this proposal.");
        //}

        public async Task VotingResults(List<PlayerVote> votes)
        {
            _state = GameState.VotingClosed;
            var sb = new StringBuilder($"The results are in.\n");
            foreach (var vote in _votes)
            {
                sb.AppendLine($"**{vote.Username}**: {(vote.Vote == Vote.Yes ? _config.Yes : _config.No)}");
            }

            sb.AppendLine($"Total in favor: {votes.Count(v => v.Vote == Vote.Yes)} - Total opposed: {votes.Count(v => v.Vote == Vote.No)}");

            if (_votes.Count(v => v.Vote == Vote.No) >= _votes.Count(v => v.Vote == Vote.Yes))
            {
                _electionTracker++;
                sb.Append($"The vote has **not** gone through. {_config.Parliament} is stalled and ");
                switch (_electionTracker)
                {
                    case 1:
                        sb.Append(_config.ThePeopleOne);
                        await _channel.SendMessage(sb.ToString());
                        break;
                    case 2:
                        sb.Append(_config.ThePeopleTwo);
                        await _channel.SendMessage(sb.ToString());
                        break;
                    case 3:
                        sb.AppendLine(_config.ThePeopleThree);
                        await _channel.SendMessage(sb.ToString());
                        await Task.Delay(10000);
                        _electionTracker = 0;
                        var pol = Deck.Pop();
                        await _channel.SendMessage(String.Format(_config.ThePeopleEnacted, pol.ToString()));
                        if (pol == PolicyType.Fascist)
                        {
                            await ResolveEffect(FascistTrack.First(b => b.IsEmpty), true);
                        }
                        else
                        {
                            await ResolveEffect(LiberalTrack.First(b => b.IsEmpty), true);
                        }
                        if (Deck.Count == 0)
                        {
                            ReshuffleDeck();
                        }
                        break;
                }
            }
            else
            {
                _lastPresident = _currentPresident;
                _currentChancellor = _chancellorNominee;
                _lastChancellor = _chancellorNominee;
                _electionTracker = 0;
                if (!FascistTrack[2].IsEmpty && !_confirmedNot.Select(u => u.Id).Contains(_currentChancellor))
                {
                    sb.Append($"The vote has gone through. Now to ask: **Are you {_config.Hitler}**?");
                    await _channel.SendMessage(sb.ToString());
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    var chosen = _channel.GetUser(_currentChancellor);
                    if (_players.Single(p => p.Role == _config.Hitler).User.Id == _currentChancellor)
                    {
                        await _channel.SendMessage($"**{chosen.Name}** is, in fact, {_config.Hitler}. {_config.FascistsWin}");
                        await EndGame();
                    }
                    else
                    {
                        _confirmedNot.Add(chosen);
                        await _channel.SendMessage($"**{chosen.Name}** is Not {_config.Hitler} (Confirmed). {_config.Parliament} can function.");
                        await DrawPolicies();
                    }
                }
                else
                {
                    sb.Append($"The vote has gone through. {_config.Parliament} can function.");
                    await _channel.SendMessage(sb.ToString());
                    await DrawPolicies();
                }
            }
        }

        public async Task DrawPolicies()
        {
            _state = GameState.PresidentPicks;

            _policies = new List<PolicyType> { Deck.Pop(), Deck.Pop(), Deck.Pop() };
            var sb = new StringBuilder($"You have drawn the following {_config.Policies}:\n");
            for (int i = 0; i < _policies.Count; i++)
            {
                sb.AppendLine($"\t**{i + 1}**: {(_policies[i] == PolicyType.Fascist ? _config.Fascist : _config.Liberal)}");
            }
            sb.Append($"Which {_config.Policy} will you discard? The other two are automatically sent to your {_config.Chancellor}.");
            await _channel.GetUser(_currentPresident).PrivateChannel.SendMessage(sb.ToString());
        }

        public async Task ChancellorPick()
        {
            _state = GameState.ChancellorPicks;
            var sb = new StringBuilder($"The {_config.President} has given you these {_config.Policies}:");
            for (int i = 0; i < _policies.Count; i++)
            {
                sb.AppendLine($"\t**{i + 1}**: {(_policies[i] == PolicyType.Fascist ? _config.Fascist : _config.Liberal)}");
            }

            if (!_vetoUnlocked)
            {
                sb.Append($"Which {_config.Policy} will you discard? The other one is automatically played to the board.");
                await _channel.GetUser(_currentChancellor).PrivateChannel.SendMessage(sb.ToString());
            }
            else
            {
                sb.Append($"Will you discard one or `veto`?");
                await _channel.GetUser(_currentChancellor).PrivateChannel.SendMessage(sb.ToString());
            }
        }

        public async Task ResolveEffect(BoardSpace space, bool peopleEnacted = false)
        {
            _state = GameState.PolicyEnacted;
            space.IsEmpty = false;

            if (peopleEnacted)
            {
                if (space.Type == BoardSpaceType.ExecutionVeto)
                {
                    _vetoUnlocked = true;
                    await _channel.SendMessage($"The {_config.President} and {_config.Chancellor} may veto from now.");
                }
                else
                {
                    await _channel.SendMessage($"No action is taken.");
                }
            }
            else
            {
                switch (space.Type)
                {
                    case BoardSpaceType.Blank:
                        await _channel.SendMessage($"Nothing happens. Next turn when players are ready.");
                        return;
                    case BoardSpaceType.Examine:
                        await _channel.SendMessage($"The {_config.President} may see the top 3 cards from the {_config.Policy} deck.");
                        var tops = String.Join(", ", new[] { Deck.ElementAt(0).ToString(), Deck.ElementAt(1).ToString(), Deck.ElementAt(2).ToString() });
                        await _channel.GetUser(_currentPresident).PrivateChannel.SendMessage($"The top 3 {_config.Policy} cards are {tops}");
                        return;
                    case BoardSpaceType.Investigate:
                        _state = GameState.Investigating;
                        await _channel.SendMessage($"The {_config.President} may investigate one player's Party affinity.");
                        return;
                    case BoardSpaceType.ChooseNextCandidate:
                        _state = GameState.SpecialElection;
                        await _channel.SendMessage($"The current {_config.President} may choose the next turn's {_config.President}.");
                        return;
                    case BoardSpaceType.Execution:
                        _state = GameState.Kill;
                        await _channel.SendMessage($"The {_config.President} may choose another player to kill.");
                        return;
                    case BoardSpaceType.ExecutionVeto:
                        _vetoUnlocked = true;
                        _state = GameState.Kill;
                        await _channel.SendMessage($"The {_config.President} may choose another player to kill. Also, the {_config.President} and {_config.Chancellor} may veto.");
                        return;
                    case BoardSpaceType.FascistWin:
                        await _channel.SendMessage(_config.FascistsWin);
                        await EndGame();
                        return;
                    case BoardSpaceType.LiberalWin:
                        await _channel.SendMessage(_config.LiberalsWin);
                        await EndGame();
                        return;
                }
            }
        }

        public async Task EndGame()
        {
            _channel.Client.MessageReceived -= ProcessMessage;
            Commands.RegisterSecHitCommands.gameOpen = false;
            var sb = new StringBuilder("The game is over.\n")
                .AppendLine($"The {_config.Fascist}s were **{String.Join("**, **", _players.Where(p => p.Party == _config.FascistParty).Select(p => p.User.Name))}**.")
                .AppendLine($"The {_config.Liberal}s were **{String.Join("**, **", _players.Where(p => p.Party == _config.LiberalParty).Select(p => p.User.Name))}**.");
            await _channel.SendMessage(sb.ToString());
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
                if (e.Channel.IsPrivate && _players.Select(p => p.User.PrivateChannel.Id).Contains(e.Channel.Id))
                {
                    switch (_state)
                    {
                        case GameState.VoteForGovernment:
                            if (!_votes.Any(p => p.Username == e.User.Name) && _players.Where(p => p.IsAlive).Select(p => p.User.Id).Contains(e.User.Id))
                            {
                                Vote v;
                                if (e.Message.Text.ToLowerInvariant() == _config.Yes.ToLowerInvariant())
                                {
                                    v = Vote.Yes;
                                }
                                else if (e.Message.Text.ToLowerInvariant() == _config.No.ToLowerInvariant())
                                {
                                    v = Vote.No;
                                }
                                else
                                {
                                    await e.Channel.SendMessage("Unnacceptable parameter.");
                                    return;
                                }
                                _votes.Add(new PlayerVote(e.User.Name, v));
                                PropertyChanged(_votes, new PropertyChangedEventArgs(nameof(_votes)));
                                while (_channel.Client.MessageQueue.Count > 10) await Task.Delay(100);

                                await e.Channel.SendMessage("Your vote has been recorded.");
                            }
                            return;
                        case GameState.PresidentPicks:
                            int i;
                            if (e.User.Id == _currentPresident && Int32.TryParse(e.Message.Text, out i))
                            {
                                switch (i)
                                {
                                    case 1:
                                    case 2:
                                    case 3:
                                        await e.Channel.SendMessage($"Removing a {(_policies[i - 1] == PolicyType.Fascist ? _config.Fascist : _config.Liberal)} {_config.Policy}.");
                                        Discards.Push(_policies[i - 1]);
                                        _policies.RemoveAt(i - 1);
                                        await _channel.SendMessage($"The {_config.President} has discarded one {_config.Policy}.");
                                        await Task.Delay(1000);
                                        await ChancellorPick();
                                        return;
                                    default:
                                        await e.Channel.SendMessage("Out of range.");
                                        return;
                                }
                            }
                            return;
                        case GameState.ChancellorPicks:
                            int j;
                            if (e.User.Id == _currentChancellor && Int32.TryParse(e.Message.Text, out j))
                            {
                                switch (j)
                                {
                                    case 1:
                                    case 2:
                                        await e.Channel.SendMessage($"Removing a {(_policies[j - 1] == PolicyType.Fascist ? _config.Fascist : _config.Liberal)} {_config.Policy}.");
                                        Discards.Push(_policies[j - 1]);
                                        _policies.RemoveAt(j - 1);
                                        await _channel.SendMessage($"The {_config.Chancellor} has discarded one {_config.Policy} and played a **{(_policies.Single() == PolicyType.Fascist ? _config.Fascist : _config.Liberal)}** {_config.Policy}.");
                                        await Task.Delay(1000);
                                        var space = (_policies.Single() == PolicyType.Fascist)
                                            ? FascistTrack.First(s => s.IsEmpty)
                                            : LiberalTrack.First(s => s.IsEmpty);
                                        await ResolveEffect(space);
                                        if (Deck.Count < 3)
                                        {
                                            ReshuffleDeck();
                                        }
                                        return;
                                    default:
                                        await e.Channel.SendMessage("Out of range.");
                                        return;
                                }
                            }
                            else if (_vetoUnlocked && e.Message.Text.ToLowerInvariant() == "veto")
                            {
                                if (!_takenVeto)
                                {
                                    _state = GameState.ChancellorVetod;
                                    await _channel.SendMessage($"The {_config.Chancellor} has opted to veto. Do you consent, Mr./Mrs. {_config.President}?");
                                    return;
                                }
                                else
                                {
                                    await e.Channel.SendMessage($"You have already attempted to veto.");
                                    return;
                                }
                            }
                            return;
                        default:
                            return;
                    }
                }
                else if (e.Channel.Id == _channel.Id && e.User.Id == _currentPresident)
                {
                    switch (_state)
                    {
                        case GameState.StartOfTurn:
                            if (e.Message.Text.ToLowerInvariant().StartsWith("nominate"))
                            {
                                var nom = e.Message.MentionedUsers.FirstOrDefault();
                                if (nom != null && _players.Select(p => p.User.Id).Contains(nom.Id))
                                {
                                    await NominatedChancellor(nom);
                                }
                            }
                            break;
                        case GameState.SpecialElection:
                            if (e.User.Id == _currentPresident && e.Message.Text.ToLowerInvariant().StartsWith("elect"))
                            {
                                var target = e.Message.MentionedUsers.FirstOrDefault();
                                if (target != null && _players.Where(p => p.IsAlive && p.User.Id != _currentChancellor && p.User.Id != _currentPresident).Select(p => p.User.Id).Contains(target.Id))
                                {
                                    _specialElected = target.Id;
                                    await _channel.SendMessage($"The {_config.President} has Special Elected **{target.Name}**.");
                                }
                                else
                                {
                                    await _channel.SendMessage("Ineligable for nomination.");
                                }
                            }
                            break;
                        case GameState.Kill:
                            if (e.User.Id == _currentPresident && e.Message.Text.ToLowerInvariant().StartsWith("kill"))
                            {
                                var target = e.Message.MentionedUsers.FirstOrDefault();
                                if (target != null && _players.Any(p => p.User.Id == target.Id))
                                {
                                    var player = _players.Single(p => p.User.Id == target.Id);
                                    player.IsAlive = false;
                                    await _channel.SendMessage(String.Format(_config.Kill, target.Name));
                                    await Task.Delay(1500);
                                    if (player.Role == _config.Hitler)
                                    {
                                        await _channel.SendMessage(String.Format(_config.HitlerWasKilled, _config.Hitler, _config.LiberalParty));
                                        await EndGame();
                                    }
                                    else
                                    {
                                        _confirmedNot.Add(target);
                                        await _channel.SendMessage(String.Format(_config.HitlerNotKilled, player.User.Name, _config.Hitler));
                                    }
                                }
                            }
                            break;
                        case GameState.Investigating:
                            if (e.User.Id == _currentPresident && e.Message.Text.ToLowerInvariant().StartsWith("investigate"))
                            {
                                var target = e.Message.MentionedUsers.FirstOrDefault();
                                if (target != null && _players.Any(p => p.User.Id == target.Id))
                                {
                                    await _channel.SendMessage($"The {_config.President} is investigating **{target.Name}**'s loyalty.");
                                    await Task.Delay(1000);
                                    var player = _players.Single(p => p.User.Id == target.Id);
                                    await _channel.GetUser(_currentPresident).PrivateChannel.SendMessage($"**{player.User.Name}** belongs to the **{player.Party}**. You are not required to answer truthfully.");
                                }
                            }
                            break;
                        case GameState.ChancellorVetod:
                            if (!_takenVeto && e.User.Id == _currentPresident && e.Message.Text.ToLowerInvariant().StartsWith("veto"))
                            {
                                var s = e.Message.Text.ToLowerInvariant().Split(' ')[1];
                                if (s == "approved")
                                {
                                    _electionTracker++;
                                    _takenVeto = true;
                                    await _channel.SendMessage($"The {_config.President} has approved the {_config.Chancellor}'s veto.");
                                }
                                else if (s == "denied")
                                {
                                    _state = GameState.ChancellorPicks;
                                    _takenVeto = true;
                                    await _channel.SendMessage($"The {_config.President} has denied veto and the {_config.Chancellor} must play.");
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                //Environment.Exit(0);
            }
        }

        private async void PropChanged(object sender, PropertyChangedEventArgs e)
        {
            var l = sender as List<PlayerVote>;
            if (l != null && l.Count == _players.Count(p => p.IsAlive))
            {
                await VotingResults(l);
            }
        }

        private void ReshuffleDeck()
        {
            Deck = new Stack<PolicyType>(Deck.Concat(Discards));
            IEnumerable<PolicyType> temp = Deck;
            for (int i = 0; i < 28; i++)
            {
                temp = temp.Shuffle();
            }
            Deck = new Stack<PolicyType>(temp);
            Discards = new Stack<PolicyType>();
        }

        public string GetGameState()
        {
            var sb = new StringBuilder($"State of the board at turn {_turn}:\n")
                .AppendLine($"Turn state is {_state.ToString()}.")
                .AppendLine($"{LiberalTrack.Count(s => !s.IsEmpty)} {_config.Liberal} {_config.Policies} passed.")
                .AppendLine($"{FascistTrack.Count(s => !s.IsEmpty)} {_config.Fascist} {_config.Policies} passed.")
                .AppendFormat(_config.ThePeopleState + '\n', (3 - _electionTracker))
                .AppendLine($"{Deck.Count} {_config.Policies} in the deck.")
                .AppendLine($"{Discards.Count} {_config.Policies} discarded.");
            foreach (var user in _confirmedNot)
            {
                sb.AppendLine($"**{user.Name}** is Not {_config.Hitler} (Confirmed).");
            }
            foreach (var player in _players.Where(p => !p.IsAlive))
            {
                sb.AppendLine($"**{player.User.Name}** is Dead.");
            }
            sb.Append($"The order of players is: ");
            foreach (var player in _players)
            {
                if (!player.IsAlive)
                {
                    sb.Append($"~~{player.User.Name}~~");
                }
                else if (player.User.Id == _currentChancellor || player.User.Id == _currentPresident)
                {
                    sb.Append($"*{player.User.Name}*");
                }
                else if (player.User.Id == _lastChancellor || (player.User.Id == _lastPresident && _players.Count > 5))
                {
                    sb.Append($"**{player.User.Name}**");
                }
                else
                {
                    sb.Append(player.User.Name);
                }

                if (player.User.Id != _players.Last().User.Id)
                {
                    sb.Append(" -> ");
                }
            }
            sb.AppendFormat($"\n(*Italic* = current {_config.President}/{_config.Chancellor}, **bold** = last {_config.President}/{_config.Chancellor}.)");

            return sb.ToString();
        }

        private enum GameState
        {
            Setup,
            StartOfTurn,
            VoteForGovernment,
            VotingClosed,
            PresidentPicks,
            ChancellorPicks,
            ChancellorVetod,
            PolicyEnacted,
            Investigating,
            SpecialElection,
            Kill
        }

        // private Timer _timer;
    }

    [Flags]
    public enum HouseRules
    {
        None = 0,
        SkipFirstElection = 1,
    }
}
