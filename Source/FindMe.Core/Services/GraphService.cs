// <copyright file="GraphService.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using FindMe.Core.DB.Entities;
    using Microsoft.Graph;
    using Microsoft.Graph.Auth;
    using Microsoft.Identity.Client;

    public class GraphService
    {
        private readonly GraphServiceClient graphClient;
        private GraphServiceClient delegatedGraphClient;

        public GraphService(string azureAdAppClientId, string tenantId, string azureAdAppKey)
        {
            IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(azureAdAppClientId)
                .WithTenantId(tenantId)
                .WithClientSecret(azureAdAppKey)
                .Build();
            ClientCredentialProvider authenticationProvider = new ClientCredentialProvider(confidentialClientApplication);
            this.graphClient = new GraphServiceClient(authenticationProvider);
        }

        public void Init(string token)
        {
            this.delegatedGraphClient = new GraphServiceClient(new DelegateAuthenticationProvider(
                requestMessage =>
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    return Task.CompletedTask;
                }));
        }

        public async Task<WorkingHours> GetUserWorkingTime(string userId)
        {
            // TimeZones?
            var user = await this.graphClient.Users[userId].Request().Select("userPrincipalName").GetAsync();
            var userPrincipalName = user.UserPrincipalName;
            var schedules = new List<string>()
            {
                userPrincipalName,
            };

            var startTime = new DateTimeTimeZone
            {
                DateTime = "2019-03-15T09:00:00",
                TimeZone = "Pacific Standard Time",
            };

            var endTime = new DateTimeTimeZone
            {
                DateTime = "2019-03-15T18:00:00",
                TimeZone = "Pacific Standard Time",
            };
            var schedule = await this.graphClient.Users[userId].Calendar.GetSchedule(schedules, endTime, startTime).Request().PostAsync();
            var workingHours = schedule.CurrentPage[0].WorkingHours;
            return workingHours;
        }

        public async Task<User> GetUser(string userId, bool includeManager = false)
        {
            var userQuery = this.graphClient.Users[userId].Request();
            if (includeManager)
            {
                userQuery = userQuery.Expand("manager");
            }

            var user = await userQuery.GetAsync();
            return user;
        }

        public async Task<List<string>> GetUserPhotos(IEnumerable<Guid> userIds, string unknowAvatarBase64)
        {
            var batchRequestContent = new BatchRequestContent();
            var mapRequests = new Dictionary<string, string>();
            foreach (var userId in userIds)
            {
                var userPhotoQuery = this.graphClient.Users[userId.ToString()].Photos["48x48"].Content.Request();
                var batchRequestId = batchRequestContent.AddBatchRequestStep(userPhotoQuery);
                mapRequests.Add(userId.ToString(), batchRequestId);
            }

            var response = await this.graphClient.Batch.Request().PostAsync(batchRequestContent);
            var userPhotos = new List<string>();
            foreach (var userId in userIds)
            {
                var photoResponse = response.GetResponseByIdAsync(mapRequests[userId.ToString()]).Result;
                if (photoResponse.IsSuccessStatusCode)
                {
                    var data = await photoResponse.Content.ReadAsStringAsync();
                    userPhotos.Add($"data:image/png;base64,{data}");
                }
                else
                {
                    userPhotos.Add(unknowAvatarBase64);
                }
            }

            return userPhotos;
        }

        public async Task<string> GetUserPhoto(string userId, string unknowAvatarBase64)
        {
            var imgDataURL = string.Empty;
            try
            {
                var userPhotoQuery = this.graphClient.Users[userId].Photos["96x96"].Content.Request();
                var userPhotoStream = await userPhotoQuery.GetAsync();

                if (userPhotoStream != null)
                {
                    var ms = new MemoryStream();
                    userPhotoStream.CopyTo(ms);
                    var buffer = ms.ToArray();
                    var result = Convert.ToBase64String(buffer);
                    imgDataURL = $"data:image/png;base64,{result}";
                }
                else
                {
                    imgDataURL = unknowAvatarBase64;
                }
            }
            catch
            {
                imgDataURL = unknowAvatarBase64;
            }

            return imgDataURL;
        }

        public async Task<string> GetUserPhones(string userId)
        {
            var user = await this.graphClient.Users[userId].Request().Select("businessPhones,mobilePhone").GetAsync();

            var userPhones = string.Empty;
            if (!string.IsNullOrEmpty(user.MobilePhone))
            {
                userPhones = user.MobilePhone;
            }

            var businnesPhones = string.Join(", ", user.BusinessPhones);
            if (!string.IsNullOrEmpty(businnesPhones))
            {
                if (string.IsNullOrEmpty(user.MobilePhone))
                {
                    userPhones = businnesPhones;
                }
                else
                {
                    userPhones += $", {businnesPhones}";
                }
            }

            return userPhones;
        }

        public async Task<UserStatusEntity> GetUserCurrentStatus(string userId)
        {
            var userPresence = await this.delegatedGraphClient.Users[userId].Presence.Request().GetAsync();
            var userStatus = new UserStatusEntity();

            switch (userPresence.Availability)
            {
                case "Available":
                case "AvailableIdle":
                case "Busy":
                case "BusyIdle":
                case "DoNotDisturb":
                    userStatus.Type = StatusType.In;
                    break;
                case "Away":
                case "BeRightBack":
                case "Offline":
                case "PresenceUnknown":
                    userStatus.Type = StatusType.Out;
                    break;
                default:
                    userStatus.Type = StatusType.Out;
                    break;
            }

            switch (userPresence.Activity)
            {
                case "Available":
                    userStatus.OtherStatus = "Available";
                    break;
                case "Away":
                    userStatus.OtherStatus = "Away";
                    break;
                case "BeRightBack":
                    userStatus.OtherStatus = "Be right back";
                    break;
                case "Busy":
                    userStatus.OtherStatus = "Busy";
                    break;
                case "DoNotDisturb":
                    userStatus.OtherStatus = "Do not disturb";
                    break;
                case "InACall":
                    userStatus.OtherStatus = "In a call";
                    break;
                case "InAConferenceCall":
                    userStatus.OtherStatus = "In a conference call";
                    break;
                case "Inactive":
                    userStatus.OtherStatus = "Inactive";
                    break;
                case "InAMeeting":
                    userStatus.OtherStatus = "In a meeting";
                    break;
                case "Offline":
                    userStatus.OtherStatus = "Offline";
                    break;
                case "OffWork":
                    userStatus.OtherStatus = "Off work";
                    break;
                case "OutOfOffice":
                    userStatus.OtherStatus = "Out of office";
                    break;
                case "PresenceUnknown":
                    userStatus.OtherStatus = "Unknown";
                    break;
                case "Presenting":
                    userStatus.OtherStatus = "Presenting";
                    break;
                case "UrgentInterruptionsOnly":
                    userStatus.OtherStatus = "Urgent interruptions only";
                    break;
                default:
                    userStatus.OtherStatus = userPresence.Activity;
                    break;
            }

            return userStatus;
        }
    }
}
