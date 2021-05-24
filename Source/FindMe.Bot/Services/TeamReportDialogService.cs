// <copyright file="TeamReportDialogService.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Services
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using FindMe.Bot.Extensions;
    using FindMe.Core.DB.Entities;

    public class TeamReportDialogService
    {
        private const string TimeFormat = "HH:mm";
        private const string DateTimeFormat = "dd MMM, HH:mm";

        public byte[] GetCsvReport(List<WeekScheduleEntity> weekSchedules, List<UserStatusEntity> userStatuses)
        {
            var report = new StringBuilder();
            report.AppendLine("Schedules:");
            report.AppendLine("Name,Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday");
            weekSchedules.ForEach(x => report.AppendLine(this.BuildWeekScheduleRow(x)));
            report.AppendLine(string.Empty);
            report.AppendLine("Statuses:");
            report.AppendLine("Name,Created,Expired Time,In/Out,Status,Location,Created By");
            userStatuses.ForEach(x => report.AppendLine(this.BuildSatusRow(x)));
            var file = Encoding.UTF8.GetBytes(report.ToString());
            return file;
        }

        private string BuildWeekScheduleRow(WeekScheduleEntity weekSchedule)
        {
            string GetDayRange(DateTimeOffset? start, DateTimeOffset? end)
            {
                return $"{(start.HasValue ? start.Value.ToString(TimeFormat) : string.Empty)} - {(end.HasValue ? end.Value.ToString(TimeFormat) : string.Empty)}";
            }

            return $"{weekSchedule.User.Name},{GetDayRange(weekSchedule.MondayStartTime, weekSchedule.MondayEndTime)},{GetDayRange(weekSchedule.TuesdayStartTime, weekSchedule.TuesdayEndTime)}," +
                $"{GetDayRange(weekSchedule.WednesdayStartTime, weekSchedule.WednesdayEndTime)},{GetDayRange(weekSchedule.ThursdayStartTime, weekSchedule.ThursdayEndTime)}," +
                $"{GetDayRange(weekSchedule.FridayStartTime, weekSchedule.FridayEndTime)},{GetDayRange(weekSchedule.SaturdayStartTime, weekSchedule.SaturdayEndTime)}," +
                $"{GetDayRange(weekSchedule.SundayStartTime, weekSchedule.SundayEndTime)}";
        }

        private string BuildSatusRow(UserStatusEntity userStatus)
        {
            var status = userStatus.Status?.Title ?? userStatus.OtherStatus;
            var location = $"{userStatus.Location?.Address} {userStatus.Location?.Phone}";
            return $"\"{userStatus.User.Name}\",\"{userStatus.Created.ToString(DateTimeFormat)}\",\"{userStatus.Expired.ToString(DateTimeFormat)}\"," +
                $"\"{userStatus.Type.GetName()}\",\"{status}\",\"{location}\",\"{userStatus.CreatedBy.Name}\"";
        }
    }
}
