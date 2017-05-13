namespace MechHisui.SecretHitler.Models
{
    internal enum GameState
    {
        Setup = 0,
        StartOfTurn = 1,
        VoteForGovernment = 2,
        VotingClosed = 3,
        PresidentPicks = 4,
        ChancellorPicks = 5,
        ChancellorVetod = 6,
        PolicyEnacted = 7,
        Investigating = 8,
        SpecialElection = 9,
        Kill = 10,
        EndOfTurn = 11,
        //CannotDM
    }
}
