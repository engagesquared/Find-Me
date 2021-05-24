// <copyright file="Constants.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace CBA.SitMan.Core
{
    public static class Constants
    {
        public const string BotServiceUrlConfigKey = "BotServiceUrl";

        public const string NotifyUserStatusExpiredRoute = "/api/notify/user/{id}/soonExpired";
        public const string NotifyUserStatusOverduedRoute = "/api/notify/user/{id}/overdue";
        public const string NotifyUserShiftsRoute = "/api/notify/user/{id}/shifts";
        public const string NotifyUserShiftStartedRoute = "/api/notify/user/{id}/shiftStarted";
    }
}
