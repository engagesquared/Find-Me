// <copyright file="EmergencyInfoCardsService.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Services
{
    using System.IO;
    using AdaptiveCards.Templating;
    using FindMe.Bot.Extensions;
    using FindMe.Bot.Models;
    using FindMe.Core.DB.Entities;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class EmergencyInfoCardsService
    {
        private const string EmergencyInfoViewCardPath = "Resources/AdaptiveCards/EmergencyInfo/EmergencyInfoView.json";
        private const string EmergencyInfoEditCardPath = "Resources/AdaptiveCards/EmergencyInfo/EmergencyInfoEdit.json";

        private const string PersonalNumberRowId = "personalNumber";
        private const string KinNumberRowId = "kinNumber";
        private const string KinRelationshipRowId = "kinRelationship";
        private const string KinNameRowId = "kinName";

        public IMessageActivity GetInfoCard(UserEntity user, bool removeActions = false)
        {
            var cardJsonText = File.ReadAllText(EmergencyInfoViewCardPath);
            var template = new AdaptiveCardTemplate(cardJsonText);
            var data = new EmergencyInfoCardData
            {
                PersonalNumber = this.ValueOrDefault(user?.PhonePersonal),
                KinName = this.ValueOrDefault(user?.NextKinName),
                KinRelationship = this.ValueOrDefault(user?.NextKinRelation),
                KinNumber = this.ValueOrDefault(user?.NextKinPhone),
                ActionsAreHidden = removeActions,
            };

            cardJsonText = template.Expand(data);
            var cardJson = JsonConvert.DeserializeObject<JObject>(cardJsonText);
            var message = cardJson.ToBotMessage();
            return message;
        }

        public IMessageActivity GetEditCard(UserEntity user)
        {
            var cardJsonText = File.ReadAllText(EmergencyInfoEditCardPath);
            var template = new AdaptiveCardTemplate(cardJsonText);
            var data = new EmergencyInfoCardData
            {
                PersonalNumber = user?.PhonePersonal ?? string.Empty,
                KinName = user?.NextKinName ?? string.Empty,
                KinRelationship = user?.NextKinRelation ?? string.Empty,
                KinNumber = user?.NextKinPhone ?? string.Empty,
            };

            cardJsonText = template.Expand(data);
            var cardJson = JsonConvert.DeserializeObject<JObject>(cardJsonText);
            var message = cardJson.ToBotMessage();
            return message;
        }

        public void SetEmergencyInfoResult(JObject formResult, UserEntity user)
        {
            user.PhonePersonal = formResult.Value<string>(PersonalNumberRowId);
            user.NextKinName = formResult.Value<string>(KinNameRowId);
            user.NextKinRelation = formResult.Value<string>(KinRelationshipRowId);
            user.NextKinPhone = formResult.Value<string>(KinNumberRowId);
        }

        private string ValueOrDefault(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "—";
            }

            return value;
        }
    }
}
