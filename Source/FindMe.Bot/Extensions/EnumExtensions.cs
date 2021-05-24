namespace FindMe.Bot.Extensions
{
    using System;

    public static class EnumExtensions
    {
        public static string GetName(this Enum val)
        {
            return Enum.GetName(val.GetType(), val);
        }
    }
}
