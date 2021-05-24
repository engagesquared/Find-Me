// <copyright file="PersonCardState.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.DialogStates
{
    public class PersonCardState
    {
        public string ChosenCommand { get; set; }

        public string PreviousMessageId { get; set; }

        public string UserAadId { get; set; }

        public PersonCardData PersonCardData { get; set; }
    }
}
