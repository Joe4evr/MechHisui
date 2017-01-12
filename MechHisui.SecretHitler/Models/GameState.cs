namespace MechHisui.SecretHitler.Models
{
    internal enum GameState
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
        Kill,
        EndOfTurn,
        //CannotDM
    }
}
