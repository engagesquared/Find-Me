// <copyright file="StatusCardData.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Models
{
    using System.Collections.Generic;

    public class StatusCardData
    {
        public string Title { get; set; }

        public string StatusType { get; set; }

        public string Status { get; set; }

        public string Comments { get; set; }

        public string Location { get; set; }

        public string Expired { get; set; }

        public string Updated { get; set; }

        public List<Dictionary<string, string>> Logs { get; set; }

        public bool SensitiveAreHidden { get; set; }

        public bool ActionsAreHidden { get; set; }

        public bool ExtendStatusIsShown { get; set; }
    }
}
