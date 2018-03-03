namespace MechHisui.SymphoXDULib
{
    public interface IMemoria
    {
        int Id { get; }
        int Rarity { get; }
        string Name { get; }
        int HP { get; }
        int Atk { get; }
        int Def { get; }
        string Effect { get; }
    }
}