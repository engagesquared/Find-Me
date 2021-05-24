// <copyright file="StatusEditCardData.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Models
{
    using System.Collections.Generic;

    public class StatusEditCardData
    {
        public string UserName { get; set; }

        public List<Dictionary<string, string>> Statuses { get; set; }

        public List<Dictionary<string, string>> Locations { get; set; }
    }
}
