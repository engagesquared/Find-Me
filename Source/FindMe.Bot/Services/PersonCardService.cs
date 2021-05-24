// <copyright file="PersonCardService.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using FindMe.Bot.DialogStates;
    using FindMe.Core.DB;
    using FindMe.Core.DB.Entities;
    using FindMe.Core.Services;
    using Microsoft.EntityFrameworkCore;

    public class PersonCardService
    {
        private const string DateFormat = "HH:mm, dd MMM yyyy";
        private readonly GraphService graphService;
        private readonly FindMeDbContext dbContext;
        private readonly UserService userService;

        public PersonCardService(FindMeDbContext dbContext, GraphService graphService, UserService userService)
        {
            this.dbContext = dbContext;
            this.graphService = graphService;
            this.userService = userService;
        }

        public async Task<PersonCardData> GetPersonCardData(string userAadId, bool isCurrentUser, bool isUserManager)
        {
            var user = await this.userService.EnsureUser(userAadId);
            var userStatus = this.dbContext.UserStatuses.Where(x => x.UserId.ToString() == userAadId && x.Created > DateTimeOffset.Now.Date && x.Expired > DateTimeOffset.Now)
                .Include(x => x.Status)
                .Include(x => x.Location)
                .OrderByDescending(x => x.Created)
                .FirstOrDefault();

            var statusTitle = string.Empty;
            var statusInOut = string.Empty;
            var statusCreated = string.Empty;
            var statusExpired = string.Empty;

            if (userStatus == null && (user.UserScheduleType == UserScheduleType.Standard || user.UserScheduleType == null))
            {
                userStatus = await this.graphService.GetUserCurrentStatus(userAadId);
            }

            if (userStatus != null)
            {
                statusTitle = userStatus.Status?.Title ?? userStatus.OtherStatus;
                statusInOut = userStatus.Type == Core.DB.Entities.StatusType.In ? "In" : "Out";
                if (userStatus.Expired != DateTimeOffset.MinValue)
                {
                    statusExpired = userStatus.Expired.ToString(DateFormat);
                }

                if (userStatus.Created != DateTimeOffset.MinValue)
                {
                    statusCreated = userStatus.Created.ToString(DateFormat);
                }
            }

            var userPhoto = await this.graphService.GetUserPhoto(userAadId, Resources.Strings.UnknownAvatarBase64);
            var userPhones = await this.graphService.GetUserPhones(userAadId);

            var latestStatuses = this.dbContext.UserStatuses.Where(x => x.UserId.ToString() == userAadId).Include(x => x.CreatedBy).OrderByDescending(x => x.Created).Take(6).ToList();
            var logs = new List<Dictionary<string, string>>();
            foreach (var status in latestStatuses)
            {
                logs.Add(new Dictionary<string, string> { { "Date", status.Created.ToString(DateFormat) }, { "UpdatedBy", status.CreatedBy.Name } });
            }

            var canSeeLocation = userStatus?.IsSensitive == false || isCurrentUser || isUserManager;
            var data = new PersonCardData
            {
                DisplayName = user.Name,
                Email = user.Email,
                JobTitle = user.JobTitle ?? string.Empty,
                Phones = userPhones ?? string.Empty,
                PhotoBase64 = userPhoto,
                EmergencyPersonalNumber = user.PhonePersonal ?? string.Empty,
                EmergencyName = user.NextKinName ?? string.Empty,
                EmergencyRelationship = user.NextKinRelation ?? string.Empty,
                EmergencyNumber = user.NextKinPhone ?? string.Empty,
                StatusInOut = statusInOut,
                StatusText = statusTitle,
                StatusLocation = userStatus?.Location?.Address ?? string.Empty,
                StatusComments = userStatus?.Comments ?? string.Empty,
                StatusExpires = statusExpired,
                StatusLastUpdated = statusCreated,
                ActionsAreHidden = false,
                IsCurrentUser = isCurrentUser,
                IsUserManager = isUserManager,
                CanSeeLocation = canSeeLocation,
                Logs = logs,
            };
            return data;
        }
    }
}
