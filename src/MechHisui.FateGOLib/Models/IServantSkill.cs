namespace MechHisui.FateGOLib
{
    public interface IActiveSkill
    {
        string SkillName { get; }
        string Rank { get; }
        string Effect { get; }
        string RankUpEffect { get; }
    }

    public interface IPassiveSkill
    {
        string SkillName { get; }
        string Rank { get; }
        string Effect { get; }
    }
}