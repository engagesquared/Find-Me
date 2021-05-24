// <copyright file="FindMeBot.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Bots
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FindMe.Bot.Dialogs;
    using FindMe.Bot.DialogStates;
    using FindMe.Bot.Resources;
    using FindMe.Bot.Services;
    using FindMe.Core.DB;
    using FindMe.Core.Services;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Teams;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Microsoft.Extensions.Logging;

    public class FindMeBot : TeamsActivityHandler
    {
        private readonly ILogger<FindMeBot> logger;
        private readonly AdaptiveCardsService adaptiveCardsService;
        private readonly UpdateHoursDialogService updateHoursDialogService;
        private readonly GraphService graphService;
        private readonly FindMeDbContext dbContext;
        private readonly UserService userService;
        private readonly ConversationState conversationState;
        private readonly UserState userState;
        private readonly RootDialog rootDialog;
        private readonly TeamReportDialog teamReportDialog;

        public FindMeBot(
            ILogger<FindMeBot> logger,
            AdaptiveCardsService adaptiveCardsService,
            UpdateHoursDialogService updateHoursDialogService,
            FindMeDbContext dbContext,
            UserState userState,
            ConversationState conversationState,
            RootDialog rootDialog,
            UserService userService,
            TeamReportDialog teamReportDialog,
            GraphService graphService)
        {
            this.logger = logger;
            this.adaptiveCardsService = adaptiveCardsService;
            this.updateHoursDialogService = updateHoursDialogService;
            this.dbContext = dbContext;
            this.userState = userState;
            this.conversationState = conversationState;
            this.rootDialog = rootDialog;
            this.userService = userService;
            this.teamReportDialog = teamReportDialog;
            this.graphService = graphService;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            // Init graph service with delegated token before any dialogs starts
            var state = await this.userState.CreateProperty<AuthTokenState>(nameof(AuthTokenState)).GetAsync(turnContext, () => null);
            if (state?.Token != null)
            {
                this.graphService.Init(state.Token);
            }

            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occurred during the turn.
            await this.conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await this.userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnTeamsMembersAddedAsync(IList<TeamsChannelAccount> teamsMembersAdded, TeamInfo teamInfo, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            // Bot installed. Send welcome message.
            if (teamsMembersAdded.Any(x => x.Id == turnContext.Activity.Recipient.Id))
            {
                await turnContext.SendActivityAsync(MessageFactory.Attachment(this.adaptiveCardsService.CreateAdaptiveCardAttachment("Resources/AdaptiveCards/WelcomeMessage.json")));

                var message = this.updateHoursDialogService.GetUpdateNotificationCard(Strings.UpdateHoursWeeklyNotificationText);
                await turnContext.SendActivityAsync(message);
            }

            await base.OnTeamsMembersAddedAsync(teamsMembersAdded, teamInfo, turnContext, cancellationToken);
        }

        protected override async Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var conversationReference = turnContext.Activity.GetConversationReference();
            await this.EnsureConversaion(conversationReference);

            var serviceUrl = conversationReference.ServiceUrl.ToLowerInvariant().Trim();
            await this.EnsureServiceUrl(serviceUrl);

            await base.OnConversationUpdateActivityAsync(turnContext, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Run the Dialog with the new message Activity.
            await this.rootDialog.RunAsync(turnContext, this.conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }

        protected override async Task OnTeamsFileConsentAcceptAsync(ITurnContext<IInvokeActivity> turnContext, FileConsentCardResponse fileConsentCardResponse, CancellationToken cancellationToken)
        {
            if (await this.teamReportDialog.ShouldRunFileProcessingAsync(turnContext))
            {
                await this.teamReportDialog.ProcessFileAcceptedAsync(turnContext, fileConsentCardResponse, cancellationToken);
            }
        }

        protected override async Task OnTeamsFileConsentDeclineAsync(ITurnContext<IInvokeActivity> turnContext, FileConsentCardResponse fileConsentCardResponse, CancellationToken cancellationToken)
        {
            if (await this.teamReportDialog.ShouldRunFileProcessingAsync(turnContext))
            {
                await this.teamReportDialog.ProcessFileDeclineAsync(turnContext, fileConsentCardResponse, cancellationToken);
            }
        }

        protected override async Task OnTeamsSigninVerifyStateAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            this.logger.LogInformation("Running dialog with signin/verifystate from an Invoke Activity.");

            // The OAuth Prompt needs to see the Invoke Activity in order to complete the login process.

            // Run the Dialog with the new message Activity.
            await this.rootDialog.RunAsync(turnContext, this.conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }

        private async Task EnsureServiceUrl(string serviceUrl)
        {
            var existingService = this.dbContext.Config.Where(x => x.Key == CBA.SitMan.Core.Constants.BotServiceUrlConfigKey).FirstOrDefault();
            if (existingService == null)
            {
                existingService = new Core.DB.Entities.ConfigEntity { Key = CBA.SitMan.Core.Constants.BotServiceUrlConfigKey, Value = serviceUrl };
                this.dbContext.Add(existingService);
                await this.dbContext.SaveChangesAsync();
            }
            else if (existingService.Value != serviceUrl)
            {
                existingService.Value = serviceUrl;
                await this.dbContext.SaveChangesAsync();
            }
        }

        private async Task EnsureConversaion(ConversationReference conversation)
        {
            var userId = new Guid(conversation.User.AadObjectId);
            var currentUser = await this.userService.EnsureUser(conversation.User.AadObjectId);
            var conversationRef = this.dbContext.ConverstaionReferences.Where(x => x.UserId == userId).FirstOrDefault();
            if (conversationRef == null)
            {
                conversationRef = new Core.DB.Entities.UserConversationReferenceEntity
                {
                    User = currentUser,
                    ConversationId = conversation.Conversation.Id,
                };
                this.dbContext.Add(conversationRef);
                await this.dbContext.SaveChangesAsync();
            }

            if (conversationRef.ConversationId != conversation.Conversation.Id || currentUser.BotUserId != conversation.User.Id)
            {
                currentUser.BotUserId = conversation.User.Id;
                conversationRef.ConversationId = conversation.Conversation.Id;
                await this.dbContext.SaveChangesAsync();
            }
        }
    }
}
