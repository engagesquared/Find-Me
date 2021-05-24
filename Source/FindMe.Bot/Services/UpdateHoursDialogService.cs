// <copyright file="UpdateHoursDialogService.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using AdaptiveCards.Templating;
    using FindMe.Bot.DialogStates;
    using FindMe.Bot.Extensions;
    using FindMe.Bot.Models;
    using FindMe.Core.DB.Entities;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class UpdateHoursDialogService
    {
        private const string UpdateHoursNotificationCardPath = "Resources/AdaptiveCards/UpdateHours/UpdateHoursNotification.json";
        private const string WorkingDaysCardPath = "Resources/AdaptiveCards/UpdateHours/WorkingDays.json";
        private const string WorkingHoursCardPath = "Resources/AdaptiveCards/UpdateHours/UpdateHoursCard.json";
        private const string SummaryCardPath = "Resources/AdaptiveCards/UpdateHours/SummaryCard.json";
        private const string TimeFormat = "HH:mm";

        public IMessageActivity SetupChangeHoursSummaryCard(WeekScheduleEntity weekSchedule)
        {
            var cardJsonText = File.ReadAllText(SummaryCardPath);
            var template = new AdaptiveCardTemplate(cardJsonText);
            var data = new WorkingHoursCardData
            {
                ScheduleType = Enum.GetName(typeof(UserScheduleType), weekSchedule.ScheduleType),
            };
            var selectedWorkingDays = this.GetWeekSchedule(weekSchedule, weekSchedule.StartDate);
            data.Days = selectedWorkingDays.Select(x => new WorkingHoursDayModel
            {
                Name = Enum.GetName(typeof(DayOfWeek), x.DayOfWeek),
                StartTime = x.StartTime?.ToString(TimeFormat) ?? string.Empty,
                EndTime = x.EndTime?.ToString(TimeFormat) ?? string.Empty,
            }).ToList();
            cardJsonText = template.Expand(data);
            var cardJson = JsonConvert.DeserializeObject<JObject>(cardJsonText);
            var message = cardJson.ToBotMessage();
            return message;
        }

        public List<DaySchedule> GetWeekSchedule(WeekScheduleEntity wse, DateTimeOffset startOfTheWeekLocal)
        {
            var schedule = new List<DaySchedule>();
            if (wse != null)
            {
                void ParseAndAddIfValid(DayOfWeek dayOfWeek, DateTimeOffset? startOfTheDay, DateTimeOffset? endOfTheDay)
                {
                    if (startOfTheDay != null || endOfTheDay != null)
                    {
                        var day = new DaySchedule { DayOfWeek = dayOfWeek };
                        schedule.Add(day);
                        if (startOfTheDay != null)
                        {
                            day.StartTime = startOfTheWeekLocal.Add(startOfTheDay.Value - wse.StartDate);
                        }

                        if (endOfTheDay != null)
                        {
                            day.EndTime = startOfTheWeekLocal.Add(endOfTheDay.Value - wse.StartDate);
                        }
                    }
                }

                ParseAndAddIfValid(DayOfWeek.Monday, wse.MondayStartTime, wse.MondayEndTime);
                ParseAndAddIfValid(DayOfWeek.Tuesday, wse.TuesdayStartTime, wse.TuesdayEndTime);
                ParseAndAddIfValid(DayOfWeek.Wednesday, wse.WednesdayStartTime, wse.WednesdayEndTime);
                ParseAndAddIfValid(DayOfWeek.Thursday, wse.ThursdayStartTime, wse.ThursdayEndTime);
                ParseAndAddIfValid(DayOfWeek.Friday, wse.FridayStartTime, wse.FridayEndTime);
                ParseAndAddIfValid(DayOfWeek.Saturday, wse.SaturdayStartTime, wse.SaturdayEndTime);
                ParseAndAddIfValid(DayOfWeek.Sunday, wse.SundayStartTime, wse.SundayEndTime);
            }

            return schedule;
        }

        public List<DaySchedule> GetWeekSchedule(Microsoft.Graph.WorkingHours graphWorkingHours, DateTimeOffset startOfTheWeekLocal)
        {
            var schedule = new List<DaySchedule>();
            if (graphWorkingHours?.DaysOfWeek?.Count() > 0)
            {
                var startTimeSpan = new TimeSpan(graphWorkingHours.StartTime.Hour, graphWorkingHours.StartTime.Minute, 0);
                var endTimeSpan = new TimeSpan(graphWorkingHours.EndTime.Hour, graphWorkingHours.EndTime.Minute, 0);
                schedule = graphWorkingHours.DaysOfWeek.Select(x =>
                new DaySchedule
                    {
                        DayOfWeek = (DayOfWeek)(int)x,
                        StartTime = startOfTheWeekLocal.AddDays((int)x + 1).Add(startTimeSpan),
                        EndTime = startOfTheWeekLocal.AddDays((int)x + 1).Add(endTimeSpan),
                    })
                .ToList();
            }

            return schedule;
        }

        public void UpdateScheduleHours(WeekScheduleEntity schedule, JObject cardResult)
        {
            var weekDaysArray = new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };
            foreach (var weekday in weekDaysArray)
            {
                var dayId = (int)weekday;
                var daysToAddToMonday = dayId > 0 ? dayId - 1 : 7;
                DateTimeOffset? startDate = null;
                DateTimeOffset? endDate = null;
                if (TimeSpan.TryParse(cardResult.Value<string>($"{dayId}StartTime"), out TimeSpan startTimeSpan))
                {
                    startDate = schedule.StartDate.AddDays(daysToAddToMonday).Add(startTimeSpan);
                }

                if (TimeSpan.TryParse(cardResult.Value<string>($"{dayId}EndTime"), out TimeSpan endTimeSpan))
                {
                    endDate = schedule.StartDate.AddDays(daysToAddToMonday).Add(endTimeSpan);
                }

                switch (weekday)
                {
                    case DayOfWeek.Sunday:
                        schedule.SundayStartTime = startDate;
                        schedule.SundayEndTime = endDate;
                        break;
                    case DayOfWeek.Monday:
                        schedule.MondayStartTime = startDate;
                        schedule.MondayEndTime = endDate;
                        break;
                    case DayOfWeek.Tuesday:
                        schedule.TuesdayStartTime = startDate;
                        schedule.TuesdayEndTime = endDate;
                        break;
                    case DayOfWeek.Wednesday:
                        schedule.WednesdayStartTime = startDate;
                        schedule.WednesdayEndTime = endDate;
                        break;
                    case DayOfWeek.Thursday:
                        schedule.ThursdayStartTime = startDate;
                        schedule.ThursdayEndTime = endDate;
                        break;
                    case DayOfWeek.Friday:
                        schedule.FridayStartTime = startDate;
                        schedule.FridayEndTime = endDate;
                        break;
                    case DayOfWeek.Saturday:
                        schedule.SaturdayStartTime = startDate;
                        schedule.SaturdayEndTime = endDate;
                        break;
                }
            }
        }

        public IMessageActivity GetUpdateNotificationCard(string messageText)
        {
            var cardJsonText = File.ReadAllText(UpdateHoursNotificationCardPath);
            var template = new AdaptiveCardTemplate(cardJsonText);
            var data = new UpdateHoursNotificationCardData
            {
                Text = messageText,
            };

            cardJsonText = template.Expand(data);
            var cardJson = JsonConvert.DeserializeObject<JObject>(cardJsonText);
            var message = cardJson.ToBotMessage();
            return message;
        }

        public IMessageActivity GetWorkingDaysCard(List<DaySchedule> selectedWorkingDays)
        {
            var cardJsonText = File.ReadAllText(WorkingDaysCardPath);
            var template = new AdaptiveCardTemplate(cardJsonText);
            var data = new WorkingDaysCardData
            {
                SelectedDaysCommaSeparated = string.Join(",", selectedWorkingDays?.Select(x => (int)x.DayOfWeek)),
            };
            cardJsonText = template.Expand(data);
            var cardJson = JsonConvert.DeserializeObject<JObject>(cardJsonText);
            var message = cardJson.ToBotMessage();
            return message;
        }

        public IMessageActivity GetWorkingHoursCard(List<DaySchedule> selectedWorkingDays)
        {
            var cardJsonText = File.ReadAllText(WorkingHoursCardPath);
            var template = new AdaptiveCardTemplate(cardJsonText);
            var data = new WorkingHoursCardData();
            data.Days = selectedWorkingDays.Select(x => new WorkingHoursDayModel
            {
                DayId = ((int)x.DayOfWeek).ToString(),
                Name = Enum.GetName(typeof(DayOfWeek), x.DayOfWeek),
                StartTime = x.StartTime?.ToString(TimeFormat),
                EndTime = x.EndTime?.ToString(TimeFormat),
            }).ToList();
            cardJsonText = template.Expand(data);
            var cardJson = JsonConvert.DeserializeObject<JObject>(cardJsonText);
            var message = cardJson.ToBotMessage();
            return message;
        }
    }
}
