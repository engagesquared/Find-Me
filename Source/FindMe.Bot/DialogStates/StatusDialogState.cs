// <copyright file="StatusDialogState.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.DialogStates
{
    using FindMe.Core.DB.Entities;

    public class StatusDialogState
    {
        public string UserIdToUpdate { get; set; }

        public string PreviousMessageId { get; set; }

        public bool CanSeeSensitiveInfo { get; set; }

        public bool ShowStatusEditForm { get; set; }

        public string ChosenCommand { get; set; }

        public long ActiveStatusId { get; set; }
    }
}
