using System;

namespace MechHisui.FateGOLib
{
    public enum ServantGender
    {
        Male,
        Female,
        NA
    }
    public static class SGEx
    {
        public static string Stringyfy(this ServantGender gender)
        {
            switch (gender)
            {
                case ServantGender.Male:
                    return "Male";
                case ServantGender.Female:
                    return "Female";
                case ServantGender.NA:
                    return "N/A";
                default:
                    return gender.ToString();
            }
        }
    }
}
