// <copyright file="WorkingHoursCardData.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>
#pragma warning disable SA1402 // File may only contain a single type

namespace FindMe.Bot.Models
{
    using System.Collections.Generic;

    public class WorkingHoursCardData
    {
        public string ScheduleType { get; set; }

        public List<WorkingHoursDayModel> Days { get; set; } = new List<WorkingHoursDayModel>();
    }

    public class WorkingHoursDayModel
    {
        public string DayId { get; set; }

        public string Name { get; set; }

        public string StartTime { get; set; }

        public string EndTime { get; set; }
    }
}
