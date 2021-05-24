// <copyright file="DateTimeUtils.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Core.Utils
{
    using System;

    public static class DateTimeUtils
    {
        /// <summary>
        /// Calculates start of the week time.
        /// </summary>
        /// <param name="date">Date for which start of the week should be calcualted.</param>
        /// <returns>Start of the week in UTC time zone.</returns>
        public static DateTime GetStartOfTheWeekUtc(DateTime? date = null)
        {
            var dateToProcess = date ?? DateTime.UtcNow;

            // Removing/adding dates for compensating different time zones effect.
            var mondayUtc = dateToProcess.ToUniversalTime().Date.AddDays(-((int)dateToProcess.ToUniversalTime().DayOfWeek)).AddDays(1);
            return mondayUtc;
        }

        public static DateTimeOffset GetStartOfTheWeek(DateTimeOffset? date = null)
        {
            var dateToProcess = date ?? DateTimeOffset.Now;

            // Removing/adding dates for compensating different time zones effect.
            var mondayDate = dateToProcess.Date.AddDays(-((int)dateToProcess.DayOfWeek)).AddDays(1);
            var mondayDateTimeOddset = new DateTimeOffset(mondayDate, dateToProcess.Offset);
            return mondayDateTimeOddset;
        }
    }
}
