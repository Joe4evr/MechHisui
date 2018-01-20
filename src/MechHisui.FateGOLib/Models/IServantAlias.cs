namespace MechHisui.FateGOLib
{
    public interface IServantAlias
    {
        string Alias { get; }
        IServantProfile Servant { get; }
    }
}