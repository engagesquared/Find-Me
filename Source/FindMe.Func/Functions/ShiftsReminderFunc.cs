// <copyright file="ShiftsReminderFunc.cs" company="Engage Squared">
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
    using Microsoft.Extensions.Logging;

    public class ShiftsReminderFunc
    {
        private const string FuncName = "ShiftsReminderFunc";

        private readonly ILogger<ShiftsReminderFunc> log;
        private readonly FindMeDbContext dbContext;
        private readonly HttpClient httpClient;

        public ShiftsReminderFunc(ILogger<ShiftsReminderFunc> log, FindMeDbContext dbContext, HttpClient httpClient)
        {
            this.log = log;
            this.dbContext = dbContext;
            this.httpClient = httpClient;
        }

        [FunctionName(FuncName)]
        public async Task RunAsync([TimerTrigger(FuncConfig.ShiftsReminderSchedule)]TimerInfo myTimer)
        {
            this.log.LogInformation($"{FuncName} started.");

            var startOfTheWeek = DateTimeUtils.GetStartOfTheWeekUtc();

            this.log.LogInformation($"Current week UTC: {startOfTheWeek}");

            this.log.LogTrace("Loading users with shift schedule for last week and empty schedule for the current week.");

            var usersToNotify = this.dbContext.Users
                .Where(x => x.UserScheduleType == UserScheduleType.Shift && !x.WeekSchedules.Any(ws => ws.ScheduleType == UserScheduleType.Shift && ws.StartDateUtc == startOfTheWeek)).ToList();

            this.log.LogTrace($"{usersToNotify.Count} users loaded.");
            await this.NotifyUsers(usersToNotify);
            this.log.LogInformation($"{FuncName} finished");
        }

        private async Task NotifyUsers(List<UserEntity> users)
        {
            foreach (var user in users)
            {
                var botNotifyPath = Constants.NotifyUserShiftsRoute.Replace("{id}", user.AadUserId.ToString());
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
