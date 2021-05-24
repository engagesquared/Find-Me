// <copyright file="UserConversationReferenceEntity.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Core.DB.Entities
{
    using System;

    public class UserConversationReferenceEntity
    {
        public string ConversationId { get; set; }

        public Guid UserId { get; set; }

        public UserEntity User { get; set; }
    }
}
