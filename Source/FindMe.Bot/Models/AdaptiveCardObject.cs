// <copyright file="AdaptiveCardObject.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Models
{
    using System.Collections.Generic;

    public class AdaptiveCardObject
    {
        public string FilePath { get; set; }

        public Dictionary<string, object> Data { get; set; }
    }
}
