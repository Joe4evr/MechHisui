using System.Threading.Tasks;
using Discord;

namespace MechHisui.TriviaService
{
    public interface ITriviaService
    {
        Channel Channel { get; }
        Task AskQuestion();
        Task EndTrivia(User winner);
        Task EndTriviaEarly();
        void StartTrivia();
    }
}