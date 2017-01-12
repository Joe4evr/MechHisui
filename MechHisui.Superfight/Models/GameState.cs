using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MechHisui.Superfight.Models
{
    internal enum GameState
    {
        Setup,
        Choosing,
        Debating,
        Voting,
        VotingClosed,
        EndOfTurn
    }
}
