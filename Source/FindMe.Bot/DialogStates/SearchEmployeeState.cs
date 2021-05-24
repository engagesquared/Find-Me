// <copyright file="SearchEmployeeState.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.DialogStates
{
    public class SearchEmployeeState
    {
        public string PreviousMessageId { get; set; }

        public string Title { get; set; }

        public string FinalMessage { get; set; }
    }
}
