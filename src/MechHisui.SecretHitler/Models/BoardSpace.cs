namespace MechHisui.SecretHitler.Models
{
    public sealed class BoardSpace
    {
        public BoardSpaceType Type { get; }

        public BoardSpace(BoardSpaceType type)
        {
            Type = type;
        }
    }
}
