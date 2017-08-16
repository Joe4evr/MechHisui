//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading.Tasks;
//using Discord.Addons.TriviaGames;
//using Discord.Commands;

//namespace Kohaku
//{
//    [Group("trivia"), Name("Trivia")]
//    public class TriviaImpl : TriviaModuleBase
//    {
//        public TriviaImpl(TriviaService service) : base(service)
//        {
//        }

//        [Command("start")]
//        public override async Task NewGameCmd(int turns)
//        {
//            // You should check if there's not already
//            // a game going on in this channel
//            if (GameInProgress)
//            {
//                await ReplyAsync("Already playing");
//                return;
//            }

//            var game = new TriviaGame(Service.TriviaData, Context.Channel, turns);
//            if (Service.AddNewGame(Context.Channel.Id, game))
//            {
//                await game.Start();
//            }
//        }

//        [Command("stop")]
//        public override async Task EndGameCmd()
//        {
//            // Here check if there IS a game going on.
//            if (GameInProgress)
//            {
//                await Game.End();
//            }
//            else
//            {
//                await ReplyAsync("No game going on.");
//            }
//        }
//    }
//}
