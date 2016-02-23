using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using JiiLib;

namespace MechHisui.SecretHitler
{
    public class Player
    {
        public User User { get; }
        public string Party { get; }
        public string Role { get; }
        public bool IsAlive { get; set; } = true;

        public Player(User user, string party, string role)
        {
            User = user;
            Party = party;
            Role = role;
        }
    }

    public class PlayerVote
    {
        public string Username { get; }
        public Vote Vote { get; }

        public PlayerVote(string name, Vote vote)
        {
            Username = name;
            Vote = vote;
        }
    }
    
    public class BoardSpace
    {
        public bool IsEmpty { get; internal set; } = true;
        public BoardSpaceType Type { get; }

        public BoardSpace(BoardSpaceType type)
        {
            Type = type;
        }
    }

    public enum BoardSpaceType
    {
        Blank,
        Examine,
        Investigate,
        ChooseNextCandidate,
        Execution,
        ExecutionVeto,
        FascistWin,
        LiberalWin
    }

    public enum Vote
    {
        No,
        Yes
    }

    public enum PolicyType
    {
        Liberal,
        Fascist
    }
}
