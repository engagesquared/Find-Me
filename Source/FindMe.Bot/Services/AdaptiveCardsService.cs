// <copyright file="AdaptiveCardsService.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Services
{
    using System.Collections.Generic;
    using System.IO;
    using AdaptiveCards.Templating;
    using FindMe.Bot.Extensions;
    using FindMe.Bot.Models;
    using FindMe.Bot.Resources;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class AdaptiveCardsService
    {
        private const string TextCardPath = "Resources/AdaptiveCards/TextCard.json";

        public Attachment CreateAdaptiveCardAttachment(string filePath)
        {
            var adaptiveCardJson = File.ReadAllText(filePath);
            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = AdaptiveCardsConstants.AdaptiveCardContentType,
                Content = JsonConvert.DeserializeObject(adaptiveCardJson),
            };
            return adaptiveCardAttachment;
        }

        public IMessageActivity GetCarouselCollection(List<AdaptiveCardObject> adaptiveCardObjects)
        {
            var attachments = new List<Attachment>();

            foreach (var cardObject in adaptiveCardObjects)
            {
                var adaptiveCardText = File.ReadAllText(cardObject.FilePath);
                AdaptiveCardTemplate template = new AdaptiveCardTemplate(adaptiveCardText);
                string processedCardText = template.Expand(cardObject.Data);
                var cardJson = JsonConvert.DeserializeObject<JObject>(processedCardText);
                var cardAttachment = new Attachment()
                {
                    ContentType = AdaptiveCardsConstants.AdaptiveCardContentType,
                    Content = cardJson,
                };

                attachments.Add(cardAttachment);
            }

            return MessageFactory.Carousel(attachments);
        }

        public IMessageActivity CreateAdaptiveCardUsingTemplate(string filePath, object data)
        {
            var adaptiveCardText = File.ReadAllText(filePath);
            return this.ProcessAdaptiveCardTemplate(adaptiveCardText, data);
        }

        public IMessageActivity GetTextCard(string text, bool isError = false)
        {
            var data = new TextCardData
            {
                Text = text,
                Color = isError ? "attention" : "default",
            };
            return this.CreateAdaptiveCardUsingTemplate(TextCardPath, data);
        }

        public IMessageActivity SetupSearchEmployeeResultsCard(
            string searchEmployeeResultsCardPath,
            List<Dictionary<string, string>> searchResults,
            bool showNextButton,
            bool showPreviousButton)
        {
            var adaptiveCardText = File.ReadAllText(searchEmployeeResultsCardPath);

            var cardTitle = string.Empty;
            if (searchResults.Count > 1)
            {
                cardTitle = "I found multiple matches, please choose one:";
            }
            else
            {
                cardTitle = "I found one match:";
            }

            var data = new
            {
                ContainerTitle = cardTitle,
                Users = searchResults,
            };
            return this.ProcessAdaptiveCardTemplate(adaptiveCardText, data);
        }

        private IMessageActivity ProcessAdaptiveCardTemplate(string adaptiveCardText, object data)
        {
            AdaptiveCardTemplate template = new AdaptiveCardTemplate(adaptiveCardText);
            string processedCardText = template.Expand(data);
            var cardJson = JsonConvert.DeserializeObject<JObject>(processedCardText);
            return cardJson.ToBotMessage();
        }
    }
}
