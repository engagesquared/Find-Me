// <copyright file="StatusCardService.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using AdaptiveCards.Templating;
    using FindMe.Bot.Extensions;
    using FindMe.Bot.Models;
    using FindMe.Bot.Resources;
    using FindMe.Core.DB.Entities;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class StatusCardService
    {
        private const string StatusEditCardPath = "Resources/AdaptiveCards/Status/UpdateStatus.json";
        private const string StatusViewCardPath = "Resources/AdaptiveCards/Status/ViewStatus.json";
        private const string UpdateStatusNotificationCardPath = "Resources/AdaptiveCards/Status/UpdateStatusNotification.json";

        private const string StatusTypeId = "statusType";
        private const string ExpiryTimeId = "expiryTime";
        private const string StatusId = "status";
        private const string OtherStatusId = "otherStatus";
        private const string CommentId = "comment";
        private const string NewLocationAddressId = "newLocationAddress";
        private const string NewLocationNumberId = "newLocationNumber";
        private const string IsSensitiveId = "newLocationSensitive";
        private const string LocationId = "location";
        private const string DateFormat = "HH:mm, dd MMM yyyy";
        private const int OtherValue = -1;

        public IMessageActivity GetViewCard(string title, bool showSensitive, bool isBaseStatus, UserStatusEntity currentStatus, List<UserStatusEntity> logStatuses, bool actionsAreHidden = false, bool extendStatusIsShown = false)
        {
            var cardJsonText = File.ReadAllText(StatusViewCardPath);
            var template = new AdaptiveCardTemplate(cardJsonText);
            var data = new StatusCardData();

            if (currentStatus != null)
            {
                if (!isBaseStatus)
                {
                    var location = currentStatus.Location?.Address ?? string.Empty;
                    if (!string.IsNullOrEmpty(currentStatus.Location?.Phone))
                    {
                        location = $"{location}, tel:{currentStatus.Location.Phone}";
                    }

                    data.StatusType = Enum.GetName(typeof(StatusType), currentStatus.Type);
                    data.Comments = currentStatus.Comments ?? string.Empty;
                    data.Location = location;
                    if (currentStatus.Expired != DateTimeOffset.MinValue)
                    {
                        data.Expired = currentStatus.Expired.ToString(DateFormat);
                    }

                    if (currentStatus.Created != DateTimeOffset.MinValue)
                    {
                        data.Updated = currentStatus.Created.ToString(DateFormat);
                    }
                }

                data.Status = currentStatus.Status?.Title ?? currentStatus.OtherStatus ?? string.Empty;
            }

            data.Title = title;
            data.SensitiveAreHidden = !showSensitive;
            data.ActionsAreHidden = actionsAreHidden;
            data.ExtendStatusIsShown = extendStatusIsShown;
            if (currentStatus == null)
            {
                data.Title = Strings.StatusIsNotSetText;
            }

            if (logStatuses?.Any() == true)
            {
                var logs = new List<Dictionary<string, string>>();
                foreach (var status in logStatuses)
                {
                    logs.Add(new Dictionary<string, string> { { "Date", status.Created.ToString(DateFormat) }, { "UpdatedBy", status.CreatedBy.Name } });
                }

                data.Logs = logs;
            }

            cardJsonText = template.Expand(data);
            var cardJson = JsonConvert.DeserializeObject<JObject>(cardJsonText);
            var message = cardJson.ToBotMessage();
            return message;
        }

        public IMessageActivity GetEditCard(UserEntity user, List<StatusEntity> statuses, List<LocationEntity> locations)
        {
            var cardJsonText = File.ReadAllText(StatusEditCardPath);
            var template = new AdaptiveCardTemplate(cardJsonText);
            var data = new StatusEditCardData();
            data.UserName = user?.Name;

            data.Statuses = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { AdaptiveCardsConstants.TitleProp, "Other" }, { AdaptiveCardsConstants.ValueProp, "-1" } },
            };
            var sortedStatuses = statuses.OrderBy(x => x.Order);
            foreach (var status in sortedStatuses)
            {
                data.Statuses.Add(new Dictionary<string, string> { { AdaptiveCardsConstants.TitleProp, status.Title }, { AdaptiveCardsConstants.ValueProp, status.Id.ToString() } });
            }

            data.Locations = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { AdaptiveCardsConstants.TitleProp, "Other" }, { AdaptiveCardsConstants.ValueProp, "-1" } },
            };
            foreach (var location in locations)
            {
                data.Locations.Add(new Dictionary<string, string> { { AdaptiveCardsConstants.TitleProp, $"{location.Address}, tel:{location.Phone ?? "-"}" }, { AdaptiveCardsConstants.ValueProp, location.Id.ToString() } });
            }

            cardJsonText = template.Expand(data);
            var cardJson = JsonConvert.DeserializeObject<JObject>(cardJsonText);
            var message = cardJson.ToBotMessage();
            return message;
        }

        public IMessageActivity GetUpdateNotificationCard(string messageText)
        {
            var cardJsonText = File.ReadAllText(UpdateStatusNotificationCardPath);
            var template = new AdaptiveCardTemplate(cardJsonText);
            var data = new UpdateStatusNotificationCardData
            {
                Text = messageText,
            };

            cardJsonText = template.Expand(data);
            var cardJson = JsonConvert.DeserializeObject<JObject>(cardJsonText);
            var message = cardJson.ToBotMessage();
            return message;
        }

        public IMessageActivity GetOverdueNotificationCard(string userName)
        {
            var message = MessageFactory.Text(Strings.StatusOverdueManagerNotification.Replace("{user}", userName));
            return message;
        }

        internal List<string> GetValidationErrors(JObject formResult)
        {
            var errors = new List<string>();
            var selectedStatus = formResult.Value<int?>(StatusId);
            var otherStatus = formResult.Value<string>(OtherStatusId);
            if (!selectedStatus.HasValue || (selectedStatus == OtherValue && string.IsNullOrEmpty(otherStatus)))
            {
                errors.Add("Status can't be empty");
            }

            var newLocation = formResult.Value<string>(NewLocationAddressId);
            var selectedLocation = formResult.Value<int?>(LocationId);
            if (!selectedLocation.HasValue || (selectedLocation == OtherValue && string.IsNullOrEmpty(newLocation)))
            {
                errors.Add("Location can't be empty");
            }

            return errors;
        }

        internal void SetUserStatus(JObject formResult, UserStatusEntity status, DateTimeOffset? localeTime)
        {
            var selectedStatus = formResult.Value<int?>(StatusId);
            var otherStatus = formResult.Value<string>(StatusId);
            if (selectedStatus > 0)
            {
                status.StatusId = selectedStatus;
            }
            else
            {
                status.OtherStatus = formResult.Value<string>(OtherStatusId);
            }

            status.Type = (StatusType)formResult.Value<int>(StatusTypeId);

            var selectedLocation = formResult.Value<int>(LocationId);
            if (selectedLocation > 0)
            {
                status.LocationId = selectedLocation;
            }
            else
            {
                status.Location = new LocationEntity
                {
                    Address = formResult.Value<string>(NewLocationAddressId),
                    Phone = formResult.Value<string>(NewLocationNumberId),
                };
            }

            if (TimeSpan.TryParse(formResult.Value<string>(ExpiryTimeId), out TimeSpan result))
            {
                var expiredDate = localeTime.HasValue ? localeTime.Value : DateTimeOffset.Now;

                // Take date part and add time value from the form
                expiredDate = expiredDate.Add(-expiredDate.TimeOfDay).Add(result);
                status.Expired = expiredDate;
            }

            status.Comments = formResult.Value<string>(CommentId);
            status.IsSensitive = formResult.Value<bool>(IsSensitiveId);
        }
    }
}
