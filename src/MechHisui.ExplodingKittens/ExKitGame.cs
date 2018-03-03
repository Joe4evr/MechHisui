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
        internal new IMessageChannel Channel => base.Channel;
        internal int DeckSize => Deck.Count;

        private ExpansioRules ExpansionRules { get; }
        private Timer ExplodeTimer { get; }
        private Timer ActionTimer { get; }

        private bool Nope { get; set; } = false;
        private int Turn { get; set; } = 0;
        private Stack<ExplodingKitttensCard> Discards { get; set; } = new Stack<ExplodingKitttensCard>();

        internal GameState State { get; private set; } = GameState.SetupGame;

        private bool Reverse { get; set; }
        private ExplodingKitttensCard QueuedAction { get; set; }
        private Stack<ExplodingKitttensCard> Deck { get; set; }

        internal ExKitGame(
            IMessageChannel channel,
            IEnumerable<ExKitPlayer> players,
            IEnumerable<ExplodingKitttensCard> deck,
            ExpansioRules expansionRules = ExpansioRules.None)
            : base(channel, players, setFirstPlayerImmediately: true)
        {
            ExpansionRules = expansionRules;
            Deck = new Stack<ExplodingKitttensCard>(deck.Shuffle(28));

            ExplodeTimer = new Timer(async _ =>
            {
                State = GameState.KittenExploded;
                await Channel.SendMessageAsync($"**{TurnPlayer.Value.User.Username}** has failed to Defuse an Exploding Kitten!").ConfigureAwait(false);
                TurnPlayer.Value.Explode();
                await CheckForWinner().ConfigureAwait(false);
            },
            null,
            Timeout.Infinite,
            Timeout.Infinite);

            ActionTimer = new Timer(async _ =>
            {
                State = GameState.Resolves;
                if (Nope)
                {
                    await Channel.SendMessageAsync("Time up! The played action is Nope'd!").ConfigureAwait(false);
                    await NextTurn().ConfigureAwait(false);
                }
                else
                {
                    await Channel.SendMessageAsync("Time up! The played action is **not** Nope'd!").ConfigureAwait(false);
                    await ResolveCardAction().ConfigureAwait(false);
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
                        player.AddToHand(Deck.Pop());
                }
            }

            foreach (var player in Players)
            {
                await player.SendHand().ConfigureAwait(false);
                await Task.Delay(500).ConfigureAwait(false);
            }
        }

        public override Task StartGame()
        {
             Deck = new Stack<ExplodingKitttensCard>(CreateDeck(Deck, Players.Count, ExpansionRules)
                 .Shuffle(28));

            return Task.CompletedTask;
        }

        public override Task NextTurn()
        {
            Turn++;
            QueuedAction = null;
            Nope = false;
            State = GameState.StartOfTurn;

            if (Turn > 1)
            {
                //if (IsFaceupImplodingKitten(card))
                //{
                //#pragma warning disable CS0219 // Variable is assigned but its value is never used
                //    var im = "Oh my, the next card is the face-up Imploding Kitten!";
                //#pragma warning restore CS0219 // Variable is assigned but its value is never used
                //}

                if (TurnPlayer.Value.IsAttacked)
                {
                    TurnPlayer.Value.IsAttacked = false;
                    return Channel.SendMessageAsync($"**{TurnPlayer.Value.User.Username}** was attacked and plays another turn.");
                }

                do TurnPlayer = Reverse
                        ? TurnPlayer.Previous
                        : TurnPlayer.Next;
                while (!TurnPlayer.Value.HasExploded);
            }

            return Channel.SendMessageAsync($"It is turn {Turn}, and **{TurnPlayer.Value.User.Username}** may play.");
        }

        private Task PlayAction(ExplodingKitttensCard card)
        {
            State = GameState.ActionPlayed;
            QueuedAction = card;
            ActionTimer.Change(TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
            return Channel.SendMessageAsync($"**{TurnPlayer.Value.User.Username}** has played the action: **{card.CardName}**");
        }

        internal Task ActionNoped(ExKitPlayer player)
        {
            Nope = !Nope;
            State = Nope ? GameState.ActionNoped : GameState.ActionYupd;
            ActionTimer.Change(TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);

            return Channel.SendMessageAsync(Nope
                ? $"**{player.User.Username}** has NOPE'd the previous action."
                : $"**{player.User.Username}** has YUP'd the previous action.");
        }

        private Task ResolveCardAction() => QueuedAction.Resolve(this);

        internal void SetState(GameState state) => State = state;

        internal IEnumerable<string> PeekTop(int number)
        {
            var r = Math.Min(Deck.Count, number);
            var buf = new string[r];
            for (int i = 0; i < r; i++)
            {
                var card = Deck.ElementAt(i);
                buf[i] =  (card is ImplodingKitten ik && ik.IsFaceUp)
                    ? card.CardName + " (face-up)"
                    : card.CardName;
            }
            return buf;
        }

        internal void Reshuffle() => Deck = new Stack<ExplodingKitttensCard>(Deck.Shuffle(32));

        public async Task Draw()
        {
            var card = Deck.Pop();
            await Task.Delay(2000).ConfigureAwait(false); //needs gradual increase from 2000 -> 2500
            if (card is ExplodingKitten)
            {
                State = GameState.KittenExploding;
                await TurnPlayer.Value.SendMessageAsync("YOU HAVE DRAWN AN EXPLODING KITTEN! DEFUSE IT QUICKLY IF YOU CAN!").ConfigureAwait(false);
                ExplodeTimer.Change(TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);
            }
            else if (IsFaceupImplodingKitten(card))
            {
                await Channel.SendMessageAsync($"**{TurnPlayer.Value.User.Username}** has drawn the face-up Imploding Kitten! Sayonara.").ConfigureAwait(false);
                TurnPlayer.Value.Explode();
                await CheckForWinner().ConfigureAwait(false);
            }
            else
            {
                await TurnPlayer.Value.SendMessageAsync($"You have drawn: **{card.CardName}**").ConfigureAwait(false);
                TurnPlayer.Value.AddToHand(card);
                await Channel.SendMessageAsync($"**{TurnPlayer.Value.User.Username}** has safely drawn a card.").ConfigureAwait(false);
                await NextTurn().ConfigureAwait(false);
            }
        }

        private bool IsFaceupImplodingKitten(ExplodingKitttensCard card)
        {
            return (ExpansionRules & ExpansioRules.ImplodingKittens) == ExpansioRules.ImplodingKittens
                    && card is ImplodingKitten ik
                    && ik.IsFaceUp;
        }

        public Task EndTurnWithoutDraw() => NextTurn();

        internal Task DefuseExplodingKitten(DefuseCard defuse)
        {
            ExplodeTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _tempCard = Discards.Pop();
            Discards.Push(defuse);
            State = GameState.KittenDefused;
            return Channel.SendMessageAsync($"**{TurnPlayer.Value.User.Username}** has successfully defused an Exploding Kitten. You may now insert this kitten at any place in the deck.");
        }

        private ExplodingKitttensCard _tempCard;

        internal void InsertExplodingKitten(uint location)
        {
            if (_tempCard != null)
            {
                Deck.InsertAt(location, _tempCard);
                _tempCard = null;
            }
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
                Discards.Push(threekind.One);
                Discards.Push(threekind.Two);
                Discards.Push(threekind.Three);
            }
            else if (card is Pair pair)
            {
                Discards.Push(pair.One);
                Discards.Push(pair.Two);
            }
            else
            {
                Discards.Push(card);
            }
        }

        internal ExKitPlayer GetFollowupPlayer()
        {
            ExKitPlayer temp;
            do temp = Reverse
                    ? TurnPlayer.Previous.Value
                    : TurnPlayer.Next.Value;
            while (!temp.HasExploded);
            return temp;
        }

        public override string GetGameState()
        {
            var x = Players.Select(p =>
            {
                return p.HasExploded ? $"~~{p.User.Username}~~" : $"{p.User.Username} ({p.HandCount} cards)";
            });

            var sb = new StringBuilder($"It is **{TurnPlayer.Value.User.Username}**'s turn")
                .AppendLine($"There are **{Deck.Count}** cards left in the Deck")
                .AppendLine($"The top card of the Discard Pile is a **{Discards.Peek().CardName}** card")
                .AppendWhen(() => IsFaceupImplodingKitten(Deck.Peek()),
                    b => b.AppendLine("The next card is the face-up Imploding Kitten!"))
                .AppendLine($"**{Players.Count(p => p.HasExploded)}** players have exploded.")
                .AppendLine($"Order of players is: {String.Join((Reverse ? " <- " : " -> "), x)}");

            return sb.ToString();
        }

        private static IEnumerable<ExplodingKitttensCard> CreateDeck(IEnumerable<ExplodingKitttensCard> cards, int players, ExpansioRules expansions)
        {
            int spcounter = players - 1;
            if (players == 2)
            {
                cards = cards.Concat(Enumerable.Repeat(new DefuseCard(), 4));
            }

            if ((expansions & ExpansioRules.ImplodingKittens) == ExpansioRules.ImplodingKittens)
            {
                cards = cards.Concat(Enumerable.Repeat(new ImplodingKitten(), 1));
                spcounter--;
            }

            cards = cards.Concat(Enumerable.Repeat(new ExplodingKitten(), spcounter));
            return cards;
        }
    }
}
