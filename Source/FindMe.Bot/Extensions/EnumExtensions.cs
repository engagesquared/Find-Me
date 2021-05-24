// <copyright file="EnumExtensions.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

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
