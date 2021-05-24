// <copyright file="PersonCardDialog.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Dialogs
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FindMe.Bot.Bots;
    using FindMe.Bot.DialogStates;
    using FindMe.Bot.Extensions;
    using FindMe.Bot.Services;
    using FindMe.Core.DB;
    using FindMe.Core.Services;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;

    public class PersonCardDialog : BaseChildDialog
    {
        private const string PersonCardPath = "Resources/AdaptiveCards/PersonCard.json";
        private const string UserAadIdKey = "userAadId";
        private const string UpdateStatusCommand = "person card update status";
        private const string RequestStatusCommand = "person card request status";
        private const string ChangeManagerCommand = "person card change manager";

        private readonly AdaptiveCardsService adaptiveCardsService;
        private readonly GraphService graphService;
        private readonly FindMeDbContext dbContext;
        private readonly ProactiveBot proactiveBot;
        private readonly PersonCardService personCardService;
        private readonly IStatePropertyAccessor<PersonCardState> stateAccessor;

        public PersonCardDialog(
            ConversationState conversationState,
            FindMeDbContext dbContext,
            ProactiveBot proactiveBot,
            AdaptiveCardsService adaptiveCardsService,
            GraphService graphService,
            PersonCardService personCardService,
            SearchEmployeeDialog searchEmployeeDialog,
            StatusDialog statusDialog)
            : base(nameof(PersonCardDialog))
        {
            this.dbContext = dbContext;
            this.proactiveBot = proactiveBot;
            this.adaptiveCardsService = adaptiveCardsService;
            this.graphService = graphService;
            this.personCardService = personCardService;

            this.stateAccessor = conversationState.CreateProperty<PersonCardState>(nameof(PersonCardState));

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                this.StartSearchEmployeeDialog,
                this.ShowEmployeeViewCard,
                this.HandleUserCommand,
                this.HandleCommandResult,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            this.AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            this.AddDialog(searchEmployeeDialog);
            this.AddDialog(statusDialog);
            this.InitialDialogId = nameof(WaterfallDialog);
        }

        protected override async Task OnEndDialogAsync(ITurnContext context, DialogInstance instance, DialogReason reason, CancellationToken cancellationToken = default)
        {
            var state = await this.stateAccessor.GetAsync(context, null);
            if (state != null && state.PersonCardData.ActionsAreHidden == false)
            {
                state.PersonCardData.ActionsAreHidden = true;
                var profileCard = this.adaptiveCardsService.CreateAdaptiveCardUsingTemplate(PersonCardPath, state.PersonCardData);
                profileCard.Id = state.PreviousMessageId;
                await context.UpdateActivityAsync(profileCard);
            }

            await this.stateAccessor.SetAsync(context, null);
            await base.OnEndDialogAsync(context, instance, reason, cancellationToken);
        }

        private async Task<DialogTurnResult> StartSearchEmployeeDialog(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.BeginDialogAsync(nameof(SearchEmployeeDialog));
        }

        private async Task<DialogTurnResult> ShowEmployeeViewCard(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var searchEmployeeResult = stepContext.Result as SearchEmployeeResult;

            if (searchEmployeeResult?.UserAadId == null)
            {
                return await stepContext.EndDialogAsync();
            }

            var userAadId = searchEmployeeResult.UserAadId;
            var user = this.dbContext.Users.Where(x => x.AadUserId.ToString() == userAadId).FirstOrDefault();
            var currentUserId = stepContext.Context.Activity.From.AadObjectId;
            var isCurrentUser = userAadId == currentUserId;
            var isUserManager = user?.ManagerId?.ToString() == currentUserId;
            var data = await this.personCardService.GetPersonCardData(userAadId, isCurrentUser, isUserManager);
            var state = new PersonCardState
            {
                UserAadId = searchEmployeeResult.UserAadId,
                PersonCardData = data,
            };

            var profileCard = this.adaptiveCardsService.CreateAdaptiveCardUsingTemplate(PersonCardPath, data);
            await stepContext.Context.SendActivityAsync(profileCard);
            state.PreviousMessageId = profileCard.Id;
            await this.stateAccessor.SetAsync(stepContext.Context, state);
            return new DialogTurnResult(DialogTurnStatus.Waiting) { ParentEnded = false };
        }

        private async Task<DialogTurnResult> HandleUserCommand(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await this.stateAccessor.GetAsync(stepContext.Context, null);
            if (state == null)
            {
                return await stepContext.EndDialogAsync();
            }

            state.PersonCardData.ActionsAreHidden = true;
            var profileCard = this.adaptiveCardsService.CreateAdaptiveCardUsingTemplate(PersonCardPath, state.PersonCardData);
            profileCard.Id = state.PreviousMessageId;
            await stepContext.Context.UpdateActivityAsync(profileCard);

            var command = stepContext.Context.Activity.GetSanitizedUserInput();
            state.ChosenCommand = command;
            await this.stateAccessor.SetAsync(stepContext.Context, state);

            switch (command)
            {
                case UpdateStatusCommand:
                    if (string.IsNullOrEmpty(state.UserAadId))
                    {
                        return new DialogTurnResult(DialogTurnStatus.Waiting) { ParentEnded = false };
                    }

                    var user = this.dbContext.Users.Where(x => x.AadUserId.ToString() == state.UserAadId).FirstOrDefault();

                    if (user == null)
                    {
                        return new DialogTurnResult(DialogTurnStatus.Waiting) { ParentEnded = false };
                    }

                    var canSeeSensitiveInfo = state.UserAadId == stepContext.Context.Activity.From.AadObjectId || user.ManagerId.ToString() == stepContext.Context.Activity.From.AadObjectId;
                    return await stepContext.BeginDialogAsync(nameof(StatusDialog), new StatusDialogState { CanSeeSensitiveInfo = canSeeSensitiveInfo, UserIdToUpdate = state.UserAadId, ShowStatusEditForm = true });
                case RequestStatusCommand:
                    return await stepContext.NextAsync();
                case ChangeManagerCommand:
                    return await stepContext.BeginDialogAsync(nameof(ChangeManagerDialog), new ChangeManagerDialogState { UserAadId = state.UserAadId });
                default:
                    return new DialogTurnResult(DialogTurnStatus.Waiting) { ParentEnded = false };
            }
        }

        private async Task<DialogTurnResult> HandleCommandResult(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await this.stateAccessor.GetAsync(stepContext.Context, null);
            if (state == null)
            {
                return await stepContext.EndDialogAsync();
            }

            switch (state.ChosenCommand)
            {
                case UpdateStatusCommand:
                    var updateStatusSuccessMessage = this.adaptiveCardsService.GetTextCard(Resources.Strings.UpdateStatusSuccessMessage);
                    await stepContext.Context.SendActivityAsync(updateStatusSuccessMessage);
                    await this.stateAccessor.SetAsync(stepContext.Context, state);
                    break;
                case RequestStatusCommand:
                    await this.proactiveBot.NotifyUserToUpdateStatus(new Guid(state.UserAadId), stepContext.Context.Activity.From.Name);
                    var statusUpdateRequestSuccessMessage = this.adaptiveCardsService.GetTextCard(Resources.Strings.StatusUpdateRequestSuccessMessage);
                    await stepContext.Context.SendActivityAsync(statusUpdateRequestSuccessMessage);
                    break;
                default:
                    break;
            }

            return await stepContext.EndDialogAsync();
        }
    }
}
