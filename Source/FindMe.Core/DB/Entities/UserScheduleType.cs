// <copyright file="UserScheduleType.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Core.DB.Entities
{
    public enum UserScheduleType
    {
        /// <summary>
        /// Standard Hours (Office workers)
        /// </summary>
        Standard = 1,

        /// <summary>
        /// Shift Pattern (Nurses)
        /// </summary>
        Shift = 2,
    }
}
