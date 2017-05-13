namespace MechHisui.SecretHitler.Models
{
    internal sealed class BoardSpace
    {
        public BoardSpaceType Type { get; }

        public BoardSpace(BoardSpaceType type)
        {
            Type = type;
        }
    }
}
