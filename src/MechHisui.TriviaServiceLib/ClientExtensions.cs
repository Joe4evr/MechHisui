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
               .AddCheck((c, u, ch) => ch.Id == long.Parse(config["FGO_trivia"]))
               .Parameter("rounds", ParameterType.Required)
               .Description("Would you like to play a game?")
               .Do(async cea =>
               {
                   if (client.GetTrivias().Any(t => t.Channel.Id == cea.Channel.Id))
                   {
                       await client.SendMessage(cea.Channel, $"Trivia already running.");
                       return;
                   }
                   int rounds;
                   if (int.TryParse(cea.Args[0], out rounds))
                   {
                       if (rounds > TriviaHelpers.Questions.Count)
                       {
                           await client.SendMessage(cea.Channel, $"Could not start trivia, too many questions specified.");
                       }
                       else
                       {
                           await client.SendMessage(cea.Channel, $"Starting trivia. Play until {rounds} points to win.");
                           var trivia = new Trivia(client, rounds, cea.Channel, config);
                           client.GetTrivias().Add(trivia);
                           trivia.StartTrivia();
                       }
                   }
                   else
                   {
                       await client.SendMessage(cea.Channel, $"Could not start trivia, parameter was not a number.");
                   }
               });
        }

        public static List<ITriviaService> _trivias = new List<ITriviaService>();
        public static List<ITriviaService> GetTrivias(this DiscordClient client) => _trivias;
    }
}
