namespace MechHisui.SymphoXDULib
{
    public interface IXduSkill
    {
        int Id { get; }
        string SkillName { get; }
        string SkillType { get; }
        int Cooldown { get; }
        string Range { get; }
        string Effect { get; }
    }
}