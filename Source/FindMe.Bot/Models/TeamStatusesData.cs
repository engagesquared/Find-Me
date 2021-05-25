// <copyright file="TeamStatusesData.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Models
{
    using System.Collections.Generic;

    public class TeamStatusesData
    {
        public List<TeamMemberStatus> Users { get; set; } = new List<TeamMemberStatus>();
    }

#pragma warning disable SA1402 // File may only contain a single type
    public class TeamMemberStatus
#pragma warning restore SA1402 // File may only contain a single type
    {
        public string ImageUrl { get; set; }

        public string Name { get; set; }

        public string Status { get; set; }

        public string Location { get; set; }

        public string Expires { get; set; }
    }
}
