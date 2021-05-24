// <copyright file="WeekScheduleEntity.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Core.DB.Entities
{
    using System;

    public class WeekScheduleEntity
    {
        public long Id { get; set; }

        public Guid UserId { get; set; }

        public UserEntity User { get; set; }

        public UserScheduleType ScheduleType { get; set; }

        public DateTimeOffset StartDateUtc { get; set; }

        public DateTimeOffset StartDate { get; set; }

        public DateTimeOffset? MondayStartTime { get; set; }

        public DateTimeOffset? MondayEndTime { get; set; }

        public DateTimeOffset? TuesdayStartTime { get; set; }

        public DateTimeOffset? TuesdayEndTime { get; set; }

        public DateTimeOffset? WednesdayStartTime { get; set; }

        public DateTimeOffset? WednesdayEndTime { get; set; }

        public DateTimeOffset? ThursdayStartTime { get; set; }

        public DateTimeOffset? ThursdayEndTime { get; set; }

        public DateTimeOffset? FridayStartTime { get; set; }

        public DateTimeOffset? FridayEndTime { get; set; }

        public DateTimeOffset? SaturdayStartTime { get; set; }

        public DateTimeOffset? SaturdayEndTime { get; set; }

        public DateTimeOffset? SundayStartTime { get; set; }

        public DateTimeOffset? SundayEndTime { get; set; }
    }
}
