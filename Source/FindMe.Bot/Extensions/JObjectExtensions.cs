// <copyright file="JObjectExtensions.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Extensions
{
    using FindMe.Bot.Resources;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json.Linq;

    public static class JObjectExtensions
    {
        public static IMessageActivity ToBotMessage(this JObject cardJson)
        {
            var message = MessageFactory.Attachment(new Attachment()
            {
                ContentType = AdaptiveCardsConstants.AdaptiveCardContentType,
                Content = cardJson,
            });
            return message;
        }
    }
}
