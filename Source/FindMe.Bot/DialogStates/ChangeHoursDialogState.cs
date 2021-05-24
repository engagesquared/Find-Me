// <copyright file="ChangeHoursDialogState.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.DialogStates
{
    using System.Collections.Generic;
    using FindMe.Core.DB.Entities;

    public class ChangeHoursDialogState
    {
        public string PreviousMessageId { get; set; }

        public List<DaySchedule> Schedule { get; set; } = new List<DaySchedule>();

        public UserScheduleType? ScheduleType { get; set; }
    }
}
