// <copyright file="UserStatusEntity.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Core.DB.Entities
{
    using System;

    public enum StatusType
    {
        /// <summary>
        /// IN
        /// </summary>
        In = 1,

        /// <summary>
        /// OUT
        /// </summary>
        Out = 2,
    }

    public class UserStatusEntity
    {
        public long Id { get; set; }

        public Guid UserId { get; set; }

        public UserEntity User { get; set; }

        public StatusType Type { get; set; }

        public int? StatusId { get; set; }

        public StatusEntity Status { get; set; }

        public string OtherStatus { get; set; }

        public long? LocationId { get; set; }

        public LocationEntity Location { get; set; }

        public bool IsSensitive { get; set; }

        public string Comments { get; set; }

        public DateTimeOffset Created { get; set; }

        public Guid CreatedById { get; set; }

        public UserEntity CreatedBy { get; set; }

        public DateTimeOffset Expired { get; set; }
    }
}
