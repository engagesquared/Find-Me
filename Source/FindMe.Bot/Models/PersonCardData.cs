// <copyright file="PersonCardData.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.DialogStates
{
    using System.Collections.Generic;

    public class PersonCardData
    {
        public string DisplayName { get; set; }

        public string Email { get; set; }

        public string JobTitle { get; set; }

        public string Phones { get; set; }

        public string PhotoBase64 { get; set; }

        public string EmergencyPersonalNumber { get; set; }

        public string EmergencyName { get; set; }

        public string EmergencyRelationship { get; set; }

        public string EmergencyNumber { get; set; }

        public string StatusInOut { get; set; }

        public string StatusText { get; set; }

        public string StatusLocation { get; set; }

        public string StatusComments { get; set; }

        public string StatusExpires { get; set; }

        public string StatusLastUpdated { get; set; }

        public List<Dictionary<string, string>> Logs { get; set; }

        public bool ActionsAreHidden { get; set; }

        public bool CanSeeLocation { get; set; }

        public bool IsCurrentUser { get; set; }

        public bool IsUserManager { get; set; }
    }
}
