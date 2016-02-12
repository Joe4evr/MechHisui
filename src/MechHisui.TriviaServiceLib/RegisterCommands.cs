using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Commands;
using MechHisui.TriviaService;

namespace MechHisui.Commands
{
    public static class ClientExtensions
    {
        public static void RegisterTriviaCommand(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Trivia'...");
            client.Commands().CreateCommand("trivia")
               .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["PrivChat"]))
               .Parameter("rounds", ParameterType.Required)
               .Description("Would you like to play a game?")
               .Do(async cea =>
               {
                   if (!TriviaHelpers.Questions.Any())
                   {
                       TriviaHelpers.InitQuestions(config);
                   }
                   if (client.GetTrivias().Any(t => t.Channel.Id == cea.Channel.Id))
                   {
                       await cea.Channel.SendMessage($"Trivia already running.");
                       return;
                   }
                   int rounds;
                   if (int.TryParse(cea.Args[0], out rounds))
                   {
                       if (rounds > TriviaHelpers.Questions.Count)
                       {
                           await cea.Channel.SendMessage($"Could not start trivia, too many questions specified.");
                       }
                       else
                       {
                           var trivia = new Trivia(client, rounds, cea.Channel, config);
                           client.GetTrivias().Add(trivia);
                           trivia.StartTrivia();
                       }
                   }
                   else
                   {
                       await cea.Channel.SendMessage($"Could not start trivia, parameter was not a number.");
                   }
               });
        }

        public static List<ITriviaService<User, Channel>> _trivias = new List<ITriviaService<User, Channel>>();
        public static List<ITriviaService<User, Channel>> GetTrivias(this DiscordClient client) => _trivias;
    }
}
