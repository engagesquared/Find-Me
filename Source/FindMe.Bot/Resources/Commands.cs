// <copyright file="Commands.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Resources
{
    using System.Collections.Generic;

    public static class Commands
    {
        public const string MyEmergencyInfo = "my emergency info";
        public const string Start = "start";
        public const string ChangeHours = "change hours";
        public const string TakeATour = "take a tour";
        public const string SearchEmployee = "search employee";
        public const string ChangeMyManager = "change my manager";
        public const string TeamReport = "team report";
        public const string UpdateStatus = "update status";

        public static List<string> AllRootCommands { get; } = new List<string>
        {
            MyEmergencyInfo,
            Start,
            ChangeHours,
            TakeATour,
            SearchEmployee,
            TeamReport,
            UpdateStatus,
            ChangeMyManager,
        };

        public static List<string> SignOutCommands { get; } = new List<string>
        {
            "sign out",
            "log out",
        };

        public static List<string> CancelCommands { get; } = new List<string>
        {
            "cancel",
            "abort",
            "stop",
        };
    }
}
