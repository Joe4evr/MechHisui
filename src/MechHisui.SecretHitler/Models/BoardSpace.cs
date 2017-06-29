namespace MechHisui.SecretHitler.Models
{
    internal sealed class BoardSpace
    {
        public BoardSpaceType BoardSpaceType { get; }

        public BoardSpace(BoardSpaceType type)
        {
            BoardSpaceType = type;
        }
    }
}
