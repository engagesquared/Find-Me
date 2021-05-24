// <copyright file="FuncConfig.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Func
{
    internal static class FuncConfig
    {
        public const string StatusReminderSchedule = "%StatusReminderSchedule%";
        public const string ShiftsReminderSchedule = "%ShiftsReminderSchedule%";
        public static readonly string DbConnectionString = System.Environment.GetEnvironmentVariable("FindMeDbConnectionString");
        public static readonly string BotBaseUrl = System.Environment.GetEnvironmentVariable("BotBaseUrl");
    }
}
