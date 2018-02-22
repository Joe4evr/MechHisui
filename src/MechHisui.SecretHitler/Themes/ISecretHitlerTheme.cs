namespace MechHisui.SecretHitler
{
    public interface ISecretHitlerTheme
    {
        string President      { get; }
        string Presidency     { get; }
        string Chancellor     { get; }
        string Chancellorship { get; }
        string Hitler         { get; }
        string Parliament     { get; }
        string FascistParty   { get; }
        string Fascist        { get; }
        string LiberalParty   { get; }
        string Liberal        { get; }
        string Policy         { get; }
        string Policies       { get; }
        string Yes            { get; }
        string No             { get; }

        string FirstStall     { get; }
        string SecondStall    { get; }
        string ThirdStall     { get; }

        string ThePeopleEnacted(string party);
        string ThePeopleState(int stalls);

        string Kill(string player);
        string HitlerNotKilled(string player);
        string HitlerWasKilled();
        
        string FascistsWin    { get; }
        string LiberalsWin    { get; }
    }
}
