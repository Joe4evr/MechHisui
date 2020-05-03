using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;
using SharedExtensions;
using MechHisui.ExplodingKittens.Cards;

namespace MechHisui.ExplodingKittens
{
    public sealed class ExKitGame : GameBase<ExKitPlayer>
    {
        internal new IMessageChannel Channel => base.Channel;
        internal int DeckSize => Deck.Count;
        internal IEnumerable<ulong> PlayerIds => Players.Select(p => p.User.Id);

        private ExpansionRules ExpansionRules { get; }
        private Timer ExplodeTimer { get; }
        private Timer ActionTimer { get; }

        private bool Nope { get; set; } = false;
        private int Turn { get; set; } = 0;
        private ExKitDiscard Discards { get; set; } = new ExKitDiscard();

        internal GameState State { get; private set; } = GameState.SetupGame;

        private bool Reverse { get; set; }
        private ExplodingKittensCard? QueuedAction { get; set; }
        private ExKitDeck Deck { get; }

        internal ExKitGame(
            IMessageChannel channel,
            IEnumerable<ExKitPlayer> players,
            IEnumerable<ExplodingKittensCard> cards,
            ExpansionRules expansionRules = ExpansionRules.None)
            : base(channel, players, setFirstPlayerImmediately: true)
        {
            ExpansionRules = expansionRules;
            Deck = new ExKitDeck(cards.Shuffle(28));

            ExplodeTimer = new Timer(async s =>
            {
                var self = (ExKitGame)s;
                self.State = GameState.KittenExploded;
                await self.Channel.SendMessageAsync($"**{self.TurnPlayer.Value.User.Username}** has failed to Defuse an Exploding Kitten!").ConfigureAwait(false);
                self.TurnPlayer.Value.Explode();
                self._tempCard = null;
                await self.CheckForWinner().ConfigureAwait(false);
            },
            this,
            Timeout.Infinite,
            Timeout.Infinite);

            ActionTimer = new Timer(async s =>
            {
                var self = (ExKitGame)s;
                if (self.Nope)
                {
                    await self.Channel.SendMessageAsync("Time up! The played action is Nope'd!").ConfigureAwait(false);
                    //await NextTurn().ConfigureAwait(false);
                }
                else
                {
                    self.State = GameState.Resolves;
                    await self.Channel.SendMessageAsync("Time up! The played action is **not** Nope'd!").ConfigureAwait(false);
                    await self.ResolveCardAction().ConfigureAwait(false);
                    self.State = GameState.MainPhase;
                }
            },
            this,
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
                        player.AddToHand(Deck.Draw());
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
            Deck.Shuffle(cards => cards.Shuffle(28));

            return Task.CompletedTask;
        }

        public override Task NextTurn()
        {
            Turn++;
            QueuedAction = null;
            Nope = false;
            State = GameState.MainPhase;

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

        internal Task PlayAction(ExplodingKittensCard card)
        {
            State = GameState.ActionPlayed;
            QueuedAction = card;
            ActionTimer.Change(TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
            return Channel.SendMessageAsync($"**{TurnPlayer.Value.User.Username}** has played the action: **{card.CardName}**");
        }

        internal Task ActionNoped(ExKitPlayer player, NopeCard nope)
        {
            Discards.Put(nope);
            Nope = !Nope;
            State = Nope ? GameState.ActionNoped : GameState.ActionYupd;
            ActionTimer.Change(TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);

            return Channel.SendMessageAsync(Nope
                ? $"**{player.User.Username}** has NOPE'd the previous action."
                : $"**{player.User.Username}** has YUP'd the previous action.");
        }

        private Task ResolveCardAction() => (QueuedAction?.Resolve(this) ?? Task.CompletedTask);

        internal void SetState(GameState state) => State = state;

        internal IEnumerable<string> PeekTop(int number)
        {
            var r = Math.Min(Deck.Count, number);
            return Deck.PeekTop(r)
                .Select(c => (c is ImplodingKitten ik && ik.IsFaceUp)
                    ? c.CardName + " (face-up)"
                    : c.CardName);
            //var buf = new string[r];
            //for (int i = 0; i < r; i++)
            //{
            //    var card = Deck.ElementAt(i);
            //    buf[i] =  (card is ImplodingKitten ik && ik.IsFaceUp)
            //        ? card.CardName + " (face-up)"
            //        : card.CardName;
            //}
            //return buf;
        }

        internal void Reshuffle() => Deck.Shuffle(cards => cards.Shuffle(32));

        internal async Task Draw()
        {
            var card = Deck.Draw();
            await Task.Delay(2000).ConfigureAwait(false); //needs gradual increase from 2000 -> 2500
            if (card is ExplodingKitten exploding)
            {
                State = GameState.KittenExploding;
                _tempCard = exploding;
                await Channel.SendMessageAsync("YOU HAVE DRAWN AN EXPLODING KITTEN! DEFUSE IT QUICKLY IF YOU CAN!").ConfigureAwait(false);
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

        private bool IsFaceupImplodingKitten(ExplodingKittensCard card)
        {
            return (ExpansionRules & ExpansionRules.ImplodingKittens) == ExpansionRules.ImplodingKittens
                    && card is ImplodingKitten ik
                    && ik.IsFaceUp;
        }

        internal Task EndTurnWithoutDraw() => NextTurn();

        internal Task DefuseExplodingKitten(DefuseCard defuse)
        {
            ExplodeTimer.Change(Timeout.Infinite, Timeout.Infinite);
            //_tempCard = Discards.Pop();
            Discards.Put(defuse);
            State = GameState.KittenDefused;
            return Channel.SendMessageAsync($"**{TurnPlayer.Value.User.Username}** has successfully defused an Exploding Kitten. You may now insert this kitten at any place in the deck.");
        }

        private ExplodingKitten? _tempCard;

        internal void InsertExplodingKitten(uint location)
        {
            if (_tempCard is ExplodingKitten exploding)
            {
                Deck.InsertAt(exploding, (int)location);
                _tempCard = null;
            }
        }

        private Task CheckForWinner()
        {
            var unexploded = Players.Where(p => !p.HasExploded);
            return unexploded.Count() > 1 ? NextTurn() : EndGame($"The game is over, **{unexploded.Single().User.Username}** is the last unexploded player.");
        }

        internal void PushToDiscards(ExplodingKittensCard card)
        {
            if (card is ThreeOfAKind threekind)
            {
                Discards.Put(threekind.One);
                Discards.Put(threekind.Two);
                Discards.Put(threekind.Three);
            }
            else if (card is Pair pair)
            {
                Discards.Put(pair.One);
                Discards.Put(pair.Two);
            }
            else
            {
                Discards.Put(card);
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
                .AppendLine($"The top card of the Discard Pile is a **{Discards.PeekTop(1).SingleOrDefault().CardName}** card")
                .AppendWhen(IsFaceupImplodingKitten(Deck.PeekTop(1).SingleOrDefault()),
                    b => b.AppendLine("The next card is the face-up Imploding Kitten!"))
                .AppendLine($"**{Players.Count(p => p.HasExploded)}** players have exploded.")
                .AppendLine($"Order of players is: {String.Join((Reverse ? " <- " : " -> "), x)}");

            return sb.ToString();
        }

        public override Embed GetGameStateEmbed()
        {
            return new EmbedBuilder
            {
                Title = $"State of the board at turn {Turn}:",
                Description = new StringBuilder($"Turn state is {State}.\n")
                    .AppendLine($"It is **{TurnPlayer.Value.User.Username}**'s turn.")
                    .AppendWhen(Discards.Count > 0,
                        b => b.AppendLine($"The top card of the Discard Pile is a **{Discards.PeekAt(0)!.CardName}**."))
                    .AppendLine($"**{Discards.AsEnumerable().Count(c => c is DefuseCard)}** Defuse cards have been played.")
                    .AppendWhen(IsFaceupImplodingKitten(Deck.PeekTop(1).SingleOrDefault()),
                        b => b.AppendLine("The next card is the face-up Imploding Kitten!"))
                    .AppendLine($"Turn order is **{(Reverse ? "Forwards" : "Backwards")}**")
                    .ToString(),
            }.AddFieldSequence(Players, (fb, p) =>
            {
                fb.IsInline = true;
                fb.Name = p.User.Username;

                var sb = new StringBuilder($"**{p.HandCount}** cards in hand.\n")
                    .AppendWhen(p.IsFavored, b => b.AppendLine("Is favored"))
                    .AppendWhen(p.IsAttacked, b => b.AppendLine("Is attacked"))
                    .AppendWhen(p.HasExploded, b => b.AppendLine("Has exploded"));

                fb.Value = sb.ToString();
            }).Build();
        }

        private static IEnumerable<ExplodingKittensCard> CreateDeck(
            IEnumerable<ExplodingKittensCard> cards,
            int players,
            ExpansionRules expansions)
        {
            int spcounter = players - 1;
            if (players == 2)
            {
                cards = cards.Concat(Enumerable.Repeat(new DefuseCard(), 4));
            }

            if ((expansions & ExpansionRules.ImplodingKittens) == ExpansionRules.ImplodingKittens)
            {
                cards = cards.Concat(Enumerable.Repeat(new ImplodingKitten(), 1));
                spcounter--;
            }

            cards = cards.Concat(Enumerable.Repeat(new ExplodingKitten(), spcounter));
            return cards;
        }
    }
}
