// <copyright file="MessageActivityExtensions.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Extensions
{
    using System.Text.RegularExpressions;
    using Microsoft.Bot.Schema;

    public static class MessageActivityExtensions
    {
        public static string GetSanitizedUserInput(this IMessageActivity messageActivity)
        {
            var message = messageActivity?.Text ?? string.Empty;

            // Remove all whitespace characters from start and end of string
            message = Regex.Replace(message, @"(^\s+)|(\s+$)", string.Empty);

            // Replace all multiple whitespace characters to 1 space character
            message = Regex.Replace(message, @"(\s{2,})", " ");
            message = message.ToLowerInvariant();
            return message;
        }
    }
}
