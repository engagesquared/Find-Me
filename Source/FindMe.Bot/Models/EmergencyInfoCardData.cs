// <copyright file="EmergencyInfoCardData.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Models
{
    public class EmergencyInfoCardData
    {
        public string PersonalNumber { get; set; }

        public string KinName { get; set; }

        public string KinRelationship { get; set; }

        public string KinNumber { get; set; }

        public bool ActionsAreHidden { get; set; }
    }
}
