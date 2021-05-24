// <copyright file="StatusReminderFunc.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Func
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using CBA.SitMan.Core;
    using FindMe.Core.DB;
    using FindMe.Core.DB.Entities;
    using FindMe.Core.Utils;
    using Microsoft.Azure.WebJobs;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    public class StatusReminderFunc
    {
        private const string FuncName = "StatusReminderFunc";

        private readonly ILogger<StatusReminderFunc> log;
        private readonly FindMeDbContext dbContext;
        private readonly HttpClient httpClient;

        public StatusReminderFunc(ILogger<StatusReminderFunc> log, FindMeDbContext dbContext, HttpClient httpClient)
        {
            this.log = log;
            this.dbContext = dbContext;
            this.httpClient = httpClient;
        }

        [FunctionName(FuncName)]
        public async Task RunAsync([TimerTrigger(FuncConfig.StatusReminderSchedule)]TimerInfo myTimer)
        {
            DateTimeOffset lastRunTimeUtc = myTimer.ScheduleStatus.Last == DateTime.MinValue ? DateTime.Now.AddMinutes(-15) : myTimer.ScheduleStatus.Last;
            DateTimeOffset currentRunTimeUtc = myTimer.ScheduleStatus.Next;

            this.log.LogInformation($"{FuncName} started.");

            var usersToNotifyShiftStarts = this.GetUsersWithShiftStartedToNotify(new TimeSpan(0, 10, 0), lastRunTimeUtc, currentRunTimeUtc);
            await this.NotifyUsers(usersToNotifyShiftStarts, Constants.NotifyUserShiftStartedRoute);

            List<UserEntity> usersToNotifyStatusExpiredSoon = this.GetShiftUsersWithActiveStatusesExpiredIn(new TimeSpan(0, -10, 0), lastRunTimeUtc, currentRunTimeUtc);
            await this.NotifyUsers(usersToNotifyStatusExpiredSoon, Constants.NotifyUserStatusExpiredRoute);

            List<UserEntity> usersToNotifyStatusOverdue = this.GetShiftUsersWithActiveStatusesExpiredIn(new TimeSpan(0, -20, 0), lastRunTimeUtc, currentRunTimeUtc);
            await this.NotifyUsers(usersToNotifyStatusOverdue, Constants.NotifyUserStatusOverduedRoute);

            this.log.LogInformation($"{FuncName} finished");
        }

        private List<UserEntity> GetUsersWithShiftStartedToNotify(TimeSpan timeShiftFromCurrentTime,  DateTimeOffset lastRunTimeUtc, DateTimeOffset currentRunTimeUtc)
        {
            var currentWeekStartUtc = DateTimeUtils.GetStartOfTheWeekUtc();
            var expiresSoonRangeEnd = currentRunTimeUtc.Add(timeShiftFromCurrentTime);
            var expiresSoonRangeStart = lastRunTimeUtc.Add(timeShiftFromCurrentTime);

            this.log.LogTrace($"Loading users without statuses and with shifts started from {expiresSoonRangeStart} to {expiresSoonRangeEnd}");

            var usersToNotify = this.dbContext.WeekSchedules
                .Where(ws => ws.ScheduleType == UserScheduleType.Shift && ws.StartDateUtc == currentWeekStartUtc)
                .Where(ws =>
                        (ws.MondayStartTime > expiresSoonRangeStart && ws.MondayStartTime <= expiresSoonRangeEnd)
                    || (ws.TuesdayStartTime > expiresSoonRangeStart && ws.TuesdayStartTime <= expiresSoonRangeEnd)
                    || (ws.WednesdayStartTime > expiresSoonRangeStart && ws.WednesdayStartTime <= expiresSoonRangeEnd)
                    || (ws.ThursdayStartTime > expiresSoonRangeStart && ws.ThursdayStartTime <= expiresSoonRangeEnd)
                    || (ws.FridayStartTime > expiresSoonRangeStart && ws.FridayStartTime <= expiresSoonRangeEnd)
                    || (ws.SaturdayStartTime > expiresSoonRangeStart && ws.SaturdayStartTime <= expiresSoonRangeEnd)
                    || (ws.SundayStartTime > expiresSoonRangeStart && ws.SundayStartTime <= expiresSoonRangeEnd))
                .Where(u => !u.User.Statuses.Any() || u.User.Statuses.OrderByDescending(s => s.Id).First().Expired < expiresSoonRangeEnd)
                .Select(x => x.User).Distinct().ToList();

            this.log.LogTrace($"{usersToNotify.Count} users loaded.");

            return usersToNotify;
        }

        private List<UserEntity> GetShiftUsersWithActiveStatusesExpiredIn(TimeSpan timeShiftFromCurrent, DateTimeOffset lastRunTimeUtc, DateTimeOffset currentRunTimeUtc)
        {
            var currentWeekStartUtc = DateTimeUtils.GetStartOfTheWeekUtc();
            var expiresSoonRangeEnd = currentRunTimeUtc.Add(timeShiftFromCurrent);
            var expiresSoonRangeStart = lastRunTimeUtc.Add(timeShiftFromCurrent);

            this.log.LogTrace($"Loading users with statuses expired from {expiresSoonRangeStart} to {expiresSoonRangeEnd}");
            var statusesToExpire = this.dbContext.Users.Where(x => x.UserScheduleType == UserScheduleType.Shift)
                .Select(x => x.Statuses.OrderByDescending(s => s.Id).FirstOrDefault())
                .Where(x => x.Expired > expiresSoonRangeStart && x.Expired <= expiresSoonRangeEnd).ToList();

            var userIds = statusesToExpire.Select(x => x.UserId).Distinct().ToList();

            this.log.LogTrace($"{userIds.Count} users loaded.");

            var userSchedules = this.dbContext.WeekSchedules.Where(ws => ws.StartDateUtc == currentWeekStartUtc && userIds.Contains(ws.UserId)).Include(x => x.User).ToList();

            var usersToNotify = new List<UserEntity>();
            var closeRangeSeconds = 300;

            foreach (var status in statusesToExpire)
            {
                var schedule = userSchedules.FirstOrDefault(x => x.UserId == status.UserId);
                if (schedule != null)
                {
                    bool IsCloseTo(DateTimeOffset? boundary)
                    {
                        return boundary.HasValue && Math.Abs((boundary.Value - status.Expired).TotalSeconds) < closeRangeSeconds;
                    }

                    var expiredInShift = (schedule.MondayStartTime < status.Expired && schedule.MondayEndTime > status.Expired)
                        || (schedule.TuesdayStartTime < status.Expired && schedule.TuesdayEndTime > status.Expired)
                        || (schedule.WednesdayStartTime < status.Expired && schedule.WednesdayEndTime > status.Expired)
                        || (schedule.ThursdayStartTime < status.Expired && schedule.ThursdayEndTime > status.Expired)
                        || (schedule.FridayStartTime < status.Expired && schedule.FridayEndTime > status.Expired)
                        || (schedule.SaturdayStartTime < status.Expired && schedule.SaturdayEndTime > status.Expired)
                        || (schedule.SundayStartTime < status.Expired && schedule.SundayEndTime > status.Expired);
                    var expiredCloseToTheEnd = IsCloseTo(schedule.MondayEndTime)
                        || IsCloseTo(schedule.TuesdayEndTime)
                        || IsCloseTo(schedule.WednesdayEndTime)
                        || IsCloseTo(schedule.ThursdayEndTime)
                        || IsCloseTo(schedule.FridayEndTime)
                        || IsCloseTo(schedule.SaturdayEndTime)
                        || IsCloseTo(schedule.SundayEndTime);
                    if (expiredInShift && !expiredCloseToTheEnd)
                    {
                        usersToNotify.Add(schedule.User);
                    }
                }
            }

            this.log.LogTrace($"{usersToNotify.Count} users with active shift and status expiration time further than {closeRangeSeconds} seconds from the end of the shift.");

            return usersToNotify;
        }

        private async Task NotifyUsers(List<UserEntity> users, string route)
        {
            foreach (var user in users)
            {
                var botNotifyPath = route.Replace("{id}", user.AadUserId.ToString());
                try
                {
                    this.log.LogInformation($"Sending request to {botNotifyPath}");
                    await this.httpClient.GetAsync(FuncConfig.BotBaseUrl + botNotifyPath);
                }
                catch (Exception ex)
                {
                    this.log.LogError(ex, $"Bot triggering error for {botNotifyPath}");
                }
            }
        }
    }
}
