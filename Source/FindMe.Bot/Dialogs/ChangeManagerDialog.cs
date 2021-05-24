// <copyright file="ChangeManagerDialog.cs" company="Engage Squared">
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
    using FindMe.Bot.Services;
    using FindMe.Core.DB;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.EntityFrameworkCore;

    public class ChangeManagerDialog : BaseChildDialog
    {
        private const string UserAadIdKey = "userAadId";

        private readonly AdaptiveCardsService adaptiveCardsService;
        private readonly FindMeDbContext dbContext;
        private readonly ProactiveBot proactiveBot;
        private readonly IStatePropertyAccessor<ChangeManagerDialogState> stateAccessor;

        public ChangeManagerDialog(ConversationState conversationState, FindMeDbContext dbContext, AdaptiveCardsService adaptiveCardsService, ProactiveBot proactiveBot, SearchEmployeeDialog searchEmployeeDialog)
            : base(nameof(ChangeManagerDialog))
        {
            this.dbContext = dbContext;
            this.adaptiveCardsService = adaptiveCardsService;
            this.proactiveBot = proactiveBot;

            this.stateAccessor = conversationState.CreateProperty<ChangeManagerDialogState>(nameof(ChangeManagerDialogState));

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                this.StartSearchEmployeeDialog,
                this.UpdateManager,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            this.AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            this.AddDialog(searchEmployeeDialog);
            this.InitialDialogId = nameof(WaterfallDialog);
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dialogContext, object options, CancellationToken cancellationToken = default)
        {
            if (options is not ChangeManagerDialogState initialState)
            {
                initialState = new ChangeManagerDialogState
                {
                    UserAadId = dialogContext.Context.Activity.From.AadObjectId,
                };
            }

            await this.stateAccessor.SetAsync(dialogContext.Context, initialState);
            return await base.OnBeginDialogAsync(dialogContext, options, cancellationToken);
        }

        private async Task<DialogTurnResult> StartSearchEmployeeDialog(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.BeginDialogAsync(nameof(SearchEmployeeDialog), new DialogStates.SearchEmployeeState { Title = Resources.Strings.ChangeManagerCardTitle, FinalMessage = Resources.Strings.ChangeManagerCardFinalMessage });
        }

        private async Task<DialogTurnResult> UpdateManager(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await this.stateAccessor.GetAsync(stepContext.Context, null);
            if (stepContext.Result == null || state.UserAadId == null)
            {
                return await stepContext.EndDialogAsync();
            }

            var searchEmployeeResult = stepContext.Result as SearchEmployeeResult;
            if (searchEmployeeResult.UserAadId == null)
            {
                return await stepContext.EndDialogAsync();
            }

            if (searchEmployeeResult.UserAadId == state.UserAadId)
            {
                var youCannotSetYourselftMessage = this.adaptiveCardsService.GetTextCard(Resources.Strings.UpdateManagerYouCannotAssignYourselfText, true);
                await stepContext.Context.SendActivityAsync(youCannotSetYourselftMessage);
                return await stepContext.EndDialogAsync();
            }

            var user = this.dbContext.Users.Where(x => x.AadUserId.ToString() == state.UserAadId).Include(x => x.Manager).FirstOrDefault();
            if (user.Manager?.AadUserId != null && (searchEmployeeResult.UserAadId == user.Manager.AadUserId.ToString()))
            {
                var selectedPersonIsAlreadyAEmployeeManager = this.adaptiveCardsService.GetTextCard(Resources.Strings.SelectedPersonIsAlreadyAEmployeeManager, true);
                await stepContext.Context.SendActivityAsync(selectedPersonIsAlreadyAEmployeeManager);
                return await stepContext.EndDialogAsync();
            }

            var previousManagerAadId = user.ManagerId?.ToString();
            var newManagerAadId = searchEmployeeResult.UserAadId;

            user.ManagerId = new Guid(searchEmployeeResult.UserAadId);
            user.ManagerIsEmpty = false;
            this.dbContext.Users.Update(user);
            await this.dbContext.SaveChangesAsync();

            var successMessage = this.adaptiveCardsService.GetTextCard(Resources.Strings.UpdateManagerSuccessText);
            await stepContext.Context.SendActivityAsync(successMessage);
            await this.proactiveBot.NotifyUsersManagerChanged(state.UserAadId, newManagerAadId, previousManagerAadId);
            return await stepContext.EndDialogAsync();
        }
    }
}
