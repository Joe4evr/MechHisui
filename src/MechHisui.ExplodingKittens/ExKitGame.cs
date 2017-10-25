using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;
using SharedExtensions;
using SharedExtensions.Collections;
using MechHisui.ExplodingKittens.Cards;

namespace MechHisui.ExplodingKittens
{
    public sealed class ExKitGame : GameBase<ExKitPlayer>
    {
        private readonly ExpansioRules _expansioRules;
        private readonly Timer _explodeTimer;
        private readonly Timer _actionTimer;

        private bool _nope = false;
        private int _turn = 0;
        private GameState _state = GameState.SetupGame;
        private ExplodingKitttensCard _queuedAction;
        private Stack<ExplodingKitttensCard> _discard = new Stack<ExplodingKitttensCard>();
        private Stack<ExplodingKitttensCard> _deck;

        internal new IMessageChannel Channel => base.Channel;

        internal bool Reverse { get; private set; }

        internal ExKitGame(
            IMessageChannel channel,
            IEnumerable<ExKitPlayer> players,
            IEnumerable<ExplodingKitttensCard> deck,
            ExpansioRules expansioRules = ExpansioRules.None)
            : base(channel, players)
        {
            _expansioRules = expansioRules;
            _deck = new Stack<ExplodingKitttensCard>(deck.Shuffle(28));

            _explodeTimer = new Timer(async _ =>
            {
                _state = GameState.KittenExploded;
                await Channel.SendMessageAsync($"**{TurnPlayer.Value.User.Username}** has failed to Defuse an Exploding Kitten!");
                TurnPlayer.Value.Explode();
                await CheckForWinner();
            },
            null,
            Timeout.Infinite,
            Timeout.Infinite);

            _actionTimer = new Timer(async _ =>
            {
                _state = GameState.Resolves;
                if (_nope)
                {
                    await Channel.SendMessageAsync("Time up! The played action is Nope'd!");
                    await NextTurn();
                }
                else
                {
                    await Channel.SendMessageAsync("Time up! The played action is **not** Nope'd!");
                    await ResolveCardAction();
                }
            },
            null,
            Timeout.Infinite,
            Timeout.Infinite);
        }

        public override async Task SetupGame()
        {
            for (int i = 0; i < 5; i++)
            {
                foreach (var player in Players)
                {
                    if (i == 0)
                        player.AddToHand(new DefuseCard());
                    else
                        player.AddToHand(_deck.Pop());
                }
            }

            foreach (var player in Players)
            {
                await player.SendHand();
                await Task.Delay(500);
            }
        }

        public override Task StartGame()
        {
             _deck = new Stack<ExplodingKitttensCard>(CreateDeck(_deck, Players.Count, _expansioRules)
                 .Shuffle(28));

            return Task.CompletedTask;
        }

        public override Task NextTurn()
        {
            _turn++;
            _queuedAction = null;
            _nope = false;
            _state = GameState.StartOfTurn;

            if (_turn > 1)
            {
                if ((_expansioRules & ExpansioRules.ImplodingKittens) == ExpansioRules.ImplodingKittens
                    && _deck.Peek() is ImplodingKitten ik && ik.IsFaceUp)
                {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
                    var im = "Oh my, the next card is the face-up Imploding Kitten!";
#pragma warning restore CS0219 // Variable is assigned but its value is never used
                }

                if (TurnPlayer.Value.IsAttacked)
                {
                    TurnPlayer.Value.IsAttacked = false;
                    return Channel.SendMessageAsync($"**{TurnPlayer.Value.User.Username}** was attacked and plays another turn.");
                }



                do TurnPlayer = Reverse ? TurnPlayer.Previous : TurnPlayer.Next;
                while (!TurnPlayer.Value.HasExploded);
            }

            return Channel.SendMessageAsync($"It is turn {_turn}, and **{TurnPlayer.Value.User.Username}** may play.");
        }

        private Task PlayAction(ExplodingKitttensCard card)
        {
            _state = GameState.ActionPlayed;
            _queuedAction = card;
            _actionTimer.Change(TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
            return Channel.SendMessageAsync($"**{TurnPlayer.Value.User.Username}** has played the action: **{card.CardName}**");
        }

        internal Task ActionNoped(IUser user)
        {
            _nope = !_nope;
            _state = _nope ? GameState.ActionNoped : GameState.ActionYupd;
            _actionTimer.Change(TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
            var player = Players.SingleOrDefault(p => p.User.Id == user.Id);
            return Channel.SendMessageAsync(_nope
                ? $"**{player.User.Username}** has NOPE'd the previous action."
                : $"**{player.User.Username}** has YUP'd the previous action.");
        }

        private Task ResolveCardAction() => _queuedAction.Resolve(this);

        internal void SetState(GameState state) => _state = state;

        internal IEnumerable<string> PeekTop(int number)
        {
            var r = Math.Min(_deck.Count, number);
            var buf = new string[r];
            for (int i = 0; i < r; i++)
            {
                var card = _deck.ElementAt(i);
                buf[i] =  (card is ImplodingKitten ik && ik.IsFaceUp)
                    ? card.CardName + " (face-up)"
                    : card.CardName;
            }
            return buf;
        }

        internal void Reshuffle() => _deck = new Stack<ExplodingKitttensCard>(_deck.Shuffle(32));

        public async Task Draw()
        {
            var card = _deck.Pop();
            await Task.Delay(2000); //needs gradual increase from 2000 -> 2500
            if (card is ExplodingKitten)
            {
                _state = GameState.KittenExploding;
                await TurnPlayer.Value.SendMessageAsync("YOU HAVE DRAWN AN EXPLODING KITTEN! DEFUSE IT QUICKLY IF YOU CAN!");
                _explodeTimer.Change(TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);
            }
            else if ((_expansioRules & ExpansioRules.ImplodingKittens) == ExpansioRules.ImplodingKittens
                    && card is ImplodingKitten ik && ik.IsFaceUp)
            {
                await Channel.SendMessageAsync($"**{TurnPlayer.Value.User.Username}** has drawn the face-up Imploding Kitten!");
                TurnPlayer.Value.Explode();
                await CheckForWinner();
            }
            else
            {
                await TurnPlayer.Value.SendMessageAsync($"You have drawn: **{card.CardName}**");
                TurnPlayer.Value.AddToHand(card);
                await Channel.SendMessageAsync($"**{TurnPlayer.Value.User.Username}** has safely drawn a card.");
                await NextTurn();
            }
        }

        public Task EndTurnWithoutDraw() => NextTurn();

        private Task DefuseExplodingKitten(ExplodingKitttensCard defuse)
        {
            _explodeTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _discard.Push(defuse);
            _state = GameState.KittenDefused;
            return Channel.SendMessageAsync($"**{TurnPlayer.Value.User.Username}** has successfully defused an Exploding Kitten. You may now insert this kitten at any place in the deck.");
        }

        private Task CheckForWinner()
        {
            var unexploded = Players.Where(p => !p.HasExploded);
            return unexploded.Count() > 1 ? NextTurn() : EndGame($"The game is over, **{unexploded.Single().User.Username}** is the last unexploded player.");
        }

        internal void PushToDiscards(ExplodingKitttensCard card)
        {
            if (card is ThreeOfAKind threekind)
            {
                _discard.Push(threekind.One);
                _discard.Push(threekind.Two);
                _discard.Push(threekind.Three);
            }
            else if (card is Pair pair)
            {
                _discard.Push(pair.One);
                _discard.Push(pair.Two);
            }
            else
            {
                _discard.Push(card);
            }
        }

        public override string GetGameState()
        {
            var x = Players.Select(p =>
            {
                return p.HasExploded ? $"~~{p.User.Username}~~" : $"{p.User.Username} ({p.HandCount} cards)";
            });

            var sb = new StringBuilder($"It is **{TurnPlayer.Value.User.Username}**'s turn")
                .AppendLine($"There are **{_deck.Count}** cards left in the Deck")
                .AppendLine($"The top card of the Discard Pile is a **{_discard.Peek().CardName}** card")
                .AppendWhen(() => ((_expansioRules & ExpansioRules.ImplodingKittens) == ExpansioRules.ImplodingKittens
                    && _deck.Peek() is ImplodingKitten ik && ik.IsFaceUp),
                    b => b.AppendLine("The next card is the face-up Imploding Kitten!"))
                .AppendLine($"**{Players.Count(p => p.HasExploded)}** players have exploded.")
                .AppendLine($"Order of players is: {String.Join((Reverse ? " <- " : " -> "), x)}");

            return sb.ToString();
        }

        private static IEnumerable<ExplodingKitttensCard> CreateDeck(IEnumerable<ExplodingKitttensCard> cards, int players, ExpansioRules expansions)
        {
            int spcounter = 1;
            if (players == 2)
            {
                cards = cards.Concat(Enumerable.Repeat(new DefuseCard(), 4));
            }

            if ((expansions & ExpansioRules.ImplodingKittens) == ExpansioRules.ImplodingKittens)
            {
                cards = cards.Concat(Enumerable.Repeat(new ImplodingKitten(), 1));
                spcounter++;
            }

            cards = cards.Concat(Enumerable.Repeat(new ExplodingKitten(), players - spcounter));
            return cards;
        }
    }
}
