// <copyright file="DaySchedule.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.DialogStates
{
    using System;

    public class DaySchedule
    {
        public DayOfWeek DayOfWeek { get; set; }

        public DateTimeOffset? StartTime { get; set; }

        public DateTimeOffset? EndTime { get; set; }
    }
}
