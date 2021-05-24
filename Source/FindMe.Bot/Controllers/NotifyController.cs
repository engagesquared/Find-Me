// <copyright file="NotifyController.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot
{
    using System;
    using System.Threading.Tasks;
    using CBA.SitMan.Core;
    using FindMe.Bot.Bots;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    public class NotifyController : ControllerBase
    {
        private readonly ProactiveBot bot;

        public NotifyController(ProactiveBot bot)
        {
            this.bot = bot;
        }

        [HttpGet]
        [Route(Constants.NotifyUserStatusExpiredRoute)]
        public async Task<StatusCodeResult> NotifySoonExpired(Guid id)
        {
            await this.bot.NotifyUserAboutStatusExpiration(id);
            return new OkResult();
        }

        [HttpGet]
        [Route(Constants.NotifyUserShiftStartedRoute)]
        public async Task<StatusCodeResult> NotifyShiftStarted(Guid id)
        {
            await this.bot.NotifyUserAboutShiftStarted(id);
            return new OkResult();
        }

        [HttpGet]
        [Route(Constants.NotifyUserStatusOverduedRoute)]
        public async Task<StatusCodeResult> NotifyOverdue(Guid id)
        {
            await this.bot.NotifyUserAboutStatusOverdue(id);
            return new OkResult();
        }

        [HttpGet]
        [Route(Constants.NotifyUserShiftsRoute)]
        public async Task<StatusCodeResult> NotifySchedule(Guid id)
        {
            await this.bot.NotifyUserAboutSchedule(id);
            return new OkResult();
        }
    }
}
