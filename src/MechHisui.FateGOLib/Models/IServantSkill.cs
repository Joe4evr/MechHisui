namespace MechHisui.FateGOLib
{
    public interface IActiveSkill
    {
        string Name { get; }
        string Rank { get; }
        string Effect { get; }
        string RankUpEffect { get; }
    }

    public interface IPassiveSkill
    {
        string Name { get; }
        string Rank { get; }
        string Effect { get; }
    }
}