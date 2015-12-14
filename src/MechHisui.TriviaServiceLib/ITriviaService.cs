using System.Threading.Tasks;
namespace MechHisui.TriviaService
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TUser">The 'User' type that represents a user.</typeparam>
    /// <typeparam name="TChannel">The 'Channel' type that represents a chatroom channel.</typeparam>
    public interface ITriviaService<TUser, TChannel>
    {
        TChannel Channel { get; }
        Task AskQuestion();
        Task EndTrivia(TUser winner);
        Task EndTriviaEarly();
        void StartTrivia();
    }
}