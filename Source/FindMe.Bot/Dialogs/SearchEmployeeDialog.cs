// <copyright file="SearchEmployeeDialog.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Dialogs
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FindMe.Bot.DialogStates;
    using FindMe.Bot.Services;
    using FindMe.Core.DB;
    using FindMe.Core.Services;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Newtonsoft.Json.Linq;

    public class SearchEmployeeDialog : BaseChildDialog
    {
        private const string SearchEmployeeCardPath = "Resources/AdaptiveCards/SearchEmployee/SearchEmployee.json";
        private const string EmployeeSearchResultsCardPath = "Resources/AdaptiveCards/SearchEmployee/EmployeeSearchResults.json";
        private const string EmployeeResultsPersonPath = "Resources/AdaptiveCards/SearchEmployee/EmployeeResultsPerson.json";
        private const string EmployeeResultsActionButtonPath = "Resources/AdaptiveCards/SearchEmployee/EmployeeResultsActionButton.json";
        private const string QueryKey = "query";
        private const string UserAadIdKey = "userAadId";

        private readonly AdaptiveCardsService adaptiveCardsService;
        private readonly GraphService graphService;
        private readonly FindMeDbContext dbContext;
        private readonly IStatePropertyAccessor<SearchEmployeeState> stateAccessor;

        public SearchEmployeeDialog(ConversationState conversationState, FindMeDbContext dbContext, AdaptiveCardsService adaptiveCardsService, GraphService graphService)
            : base(nameof(SearchEmployeeDialog))
        {
            this.dbContext = dbContext;
            this.adaptiveCardsService = adaptiveCardsService;
            this.graphService = graphService;

            this.stateAccessor = conversationState.CreateProperty<SearchEmployeeState>(nameof(SearchEmployeeState));

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                this.ShowSearchEmployeeCard,
                this.ShowEmployeeSearchResults,
                this.ReturnDialogResult,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            this.AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            this.InitialDialogId = nameof(WaterfallDialog);
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dialogContext, object options, CancellationToken cancellationToken = default)
        {
            if (options is not SearchEmployeeState initialState)
            {
                initialState = new SearchEmployeeState
                {
                    Title = Resources.Strings.SearchEmployeeCardTitle,
                    FinalMessage = Resources.Strings.SearchEmployeeCardFinalMessage,
                };
            }

            await this.stateAccessor.SetAsync(dialogContext.Context, initialState);
            return await base.OnBeginDialogAsync(dialogContext, options, cancellationToken);
        }

        protected override async Task OnEndDialogAsync(ITurnContext context, DialogInstance instance, DialogReason reason, CancellationToken cancellationToken = default)
        {
            var state = await this.stateAccessor.GetAsync(context, null);
            var finalMessage = this.adaptiveCardsService.GetTextCard(state.FinalMessage);
            finalMessage.Id = state.PreviousMessageId;
            await context.UpdateActivityAsync(finalMessage);
            await base.OnEndDialogAsync(context, instance, reason, cancellationToken);
        }

        private async Task<DialogTurnResult> ShowSearchEmployeeCard(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await this.stateAccessor.GetAsync(stepContext.Context, null);
            var searchEmployeeCardAttachment = this.adaptiveCardsService.CreateAdaptiveCardUsingTemplate(SearchEmployeeCardPath, state);
            await stepContext.Context.SendActivityAsync(searchEmployeeCardAttachment);
            state.PreviousMessageId = searchEmployeeCardAttachment.Id;
            await this.stateAccessor.SetAsync(stepContext.Context, state);
            return new DialogTurnResult(DialogTurnStatus.Waiting) { ParentEnded = false };
        }

        private async Task<DialogTurnResult> ShowEmployeeSearchResults(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await this.stateAccessor.GetAsync(stepContext.Context, null);
            if (stepContext.Context.Activity.Value == null)
            {
                return await stepContext.EndDialogAsync();
            }

            var searchQuery = (stepContext.Context.Activity.Value as JObject)[QueryKey].ToString();
            if (string.IsNullOrEmpty(searchQuery))
            {
                state.FinalMessage = Resources.Strings.NoResultsMessage;
                await this.stateAccessor.SetAsync(stepContext.Context, state);
                return await stepContext.EndDialogAsync();
            }

            var searchResults = new List<Dictionary<string, string>>();

            var response = this.dbContext.Users.Where(x => x.Name.Contains(searchQuery) || x.JobTitle.Contains(searchQuery) || x.EmailNamePart.Contains(searchQuery));
            var users = response.Take(4).ToList(); // TODO: Add pagination

            if (users.Count > 0)
            {
                var aadUserIds = users.Select(x => x.AadUserId).ToList();
                var userPhotos = await this.graphService.GetUserPhotos(aadUserIds, Resources.Strings.UnknownAvatarBase64);
                for (int i = 0; i < users.Count; i++)
                {
                    var userCardResult = new Dictionary<string, string>
                    {
                        { "id", users[i].AadUserId.ToString() },
                        { "displayName", users[i].Name ?? string.Empty },
                        { "jobTitle", users[i].JobTitle ?? string.Empty },
                        { "imageUrl", userPhotos[i] },
                    };
                    searchResults.Add(userCardResult);
                }

                var searchEmployeeResultsCardAttachment = this.adaptiveCardsService.SetupSearchEmployeeResultsCard(
                    EmployeeSearchResultsCardPath,
                    searchResults,
                    false,  // use for pagination
                    false); // use for pagination
                searchEmployeeResultsCardAttachment.Id = state.PreviousMessageId;
                await stepContext.Context.UpdateActivityAsync(searchEmployeeResultsCardAttachment);
                return new DialogTurnResult(DialogTurnStatus.Waiting) { ParentEnded = false };
            }

            state.FinalMessage = Resources.Strings.NoResultsMessage;
            await this.stateAccessor.SetAsync(stepContext.Context, state);
            return await stepContext.EndDialogAsync();
        }

        private async Task<DialogTurnResult> ReturnDialogResult(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await this.stateAccessor.GetAsync(stepContext.Context, null);
            if (stepContext.Context.Activity.Value == null)
            {
                return await stepContext.EndDialogAsync();
            }

            var userId = (stepContext.Context.Activity.Value as JObject)[UserAadIdKey]?.ToString();
            return await stepContext.EndDialogAsync(new SearchEmployeeResult { UserAadId = userId });
        }
    }
}
