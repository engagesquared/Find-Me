// <copyright file="TeamReportDialogState.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.DialogStates
{
    using System;

    public class TeamReportDialogState
    {
        public byte[] ReportContent { get; set; }

        public string PreviousMessageId { get; set; }
    }
}
