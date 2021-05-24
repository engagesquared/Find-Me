// <copyright file="UserEntity.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Core.DB.Entities
{
    using System;
    using System.Collections.Generic;

    public class UserEntity
    {
        public Guid AadUserId { get; set; }

        public UserScheduleType? UserScheduleType { get; set; }

        public string BotUserId { get; set; }

        public string Name { get; set; }

        public string JobTitle { get; set; }

        public string Email { get; set; }

        public string EmailNamePart { get; set; }

        public string PhonePersonal { get; set; }

        public string NextKinPhone { get; set; }

        public string NextKinRelation { get; set; }

        public string NextKinName { get; set; }

        public Guid? ManagerId { get; set; }

        public UserEntity Manager { get; set; }

        public bool ManagerIsEmpty { get; set; }

        public List<UserEntity> Reporters { get; set; }

        public List<UserStatusEntity> Statuses { get; set; }

        public List<WeekScheduleEntity> WeekSchedules { get; set; }
    }
}
