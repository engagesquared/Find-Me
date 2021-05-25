// <copyright file="RootDialog.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FindMe.Bot.DialogStates;
    using FindMe.Bot.Extensions;
    using FindMe.Bot.Models;
    using FindMe.Bot.Resources;
    using FindMe.Bot.Services;
    using FindMe.Core.Services;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Schema;

    public class RootDialog : ComponentDialog
    {
        private readonly AdaptiveCardsService adaptiveCardsService;
        private readonly GraphService graphService;
        private readonly IStatePropertyAccessor<RootDialogState> dialogStateAccessor;
        private readonly IStatePropertyAccessor<AuthTokenState> authTokenStateAccessor;
        private readonly string connectionName;
        private readonly AppSettings appSettings;

        public RootDialog(
            AppSettings appSettings,
            UserState userState,
            ConversationState conversationState,
            AdaptiveCardsService adaptiveCardsService,
            GraphService graphService,
            EmergencyInfoDialog setEmergencyInfoDialog,
            ChangeHoursDialog changeHoursDialog,
            StatusDialog statusDialog,
            PersonCardDialog personCardDialog,
            TeamStatusDialog teamStatusDialog,
            TeamReportDialog teamReportDialog,
            ChangeManagerDialog changeMyManagerDialog)
            : base(nameof(RootDialog))
        {
            this.adaptiveCardsService = adaptiveCardsService;
            this.graphService = graphService;
            this.connectionName = appSettings.AadUserAppConnectionName;
            this.dialogStateAccessor = conversationState.CreateProperty<RootDialogState>(nameof(RootDialogState));
            this.authTokenStateAccessor = userState.CreateProperty<AuthTokenState>(nameof(AuthTokenState));
            this.appSettings = appSettings;

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                this.LoginStepAsync,
                this.TransportStepAsync,
                this.ChildDialogEndStepAsync,
            };
            this.AddDialog(new OAuthPrompt(
                nameof(OAuthPrompt),
                new OAuthPromptSettings
                {
                    ConnectionName = this.connectionName,
                    Text = Strings.SignInCardTitle,
                    Title = Strings.SignInButtonText,
                    Timeout = 300000, // User has 5 minutes to login
                }));

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            this.AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            this.AddDialog(setEmergencyInfoDialog);
            this.AddDialog(changeHoursDialog);
            this.AddDialog(statusDialog);
            this.AddDialog(teamReportDialog);
            this.AddDialog(teamStatusDialog);
            this.AddDialog(personCardDialog);
            this.AddDialog(changeMyManagerDialog);

            this.InitialDialogId = nameof(WaterfallDialog);
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken)
        {
            var result = await this.InterruptAsync(innerDc, cancellationToken);
            if (result != null)
            {
                return result;
            }

            return await base.OnBeginDialogAsync(innerDc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            var result = await this.InterruptAsync(innerDc, cancellationToken);
            if (result != null)
            {
                return result;
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        private async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var command = stepContext.Context.Activity.GetSanitizedUserInput();
            await this.dialogStateAccessor.SetAsync(stepContext.Context, new RootDialogState { OriginalUserCommand = command });
            return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
        }

        private async Task<DialogTurnResult> TransportStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var turnContext = stepContext.Context;
            var tokenResponse = stepContext.Result as TokenResponse;
            if (tokenResponse != null)
            {
                await this.authTokenStateAccessor.SetAsync(turnContext, new AuthTokenState { Token = tokenResponse.Token });
                this.graphService.Init(tokenResponse.Token);
                var command = turnContext.Activity.GetSanitizedUserInput();

                // If we were interupted by sign-in card, we lost the original user's input context. Take it from state.
                if (turnContext?.Activity?.Type != ActivityTypes.Message)
                {
                    var state = await this.dialogStateAccessor.GetAsync(turnContext, () => null);
                    command = state?.OriginalUserCommand;
                }

                switch (command)
                {
                    case Commands.Start:
                        await turnContext.SendActivityAsync(MessageFactory.Attachment(this.adaptiveCardsService.CreateAdaptiveCardAttachment("Resources/AdaptiveCards/WelcomeMessage.json")));
                        return await stepContext.EndDialogAsync();
                    case Commands.MyEmergencyInfo:
                        return await stepContext.BeginDialogAsync(nameof(EmergencyInfoDialog));
                    case Commands.UpdateStatus:
                        return await stepContext.BeginDialogAsync(nameof(StatusDialog));
                    case Commands.ChangeHours:
                        return await stepContext.BeginDialogAsync(nameof(ChangeHoursDialog));
                    case Commands.TeamReport:
                        return await stepContext.BeginDialogAsync(nameof(TeamReportDialog));
                    case Commands.TeamStatus:
                        return await stepContext.BeginDialogAsync(nameof(TeamStatusDialog));
                    case Commands.TakeATour:
                        var carouselCardList = new List<AdaptiveCardObject>
                    {
                        new AdaptiveCardObject { FilePath = "Resources/AdaptiveCards/Carousel/SearchEmployee.json", Data = new Dictionary<string, object> { { "BackgroundImageUrl", $"{this.appSettings.HostBaseUrl}/Images/search-employee.jpg" } } },
                        new AdaptiveCardObject { FilePath = "Resources/AdaptiveCards/Carousel/UpdateStatus.json", Data = new Dictionary<string, object> { { "BackgroundImageUrl", $"{this.appSettings.HostBaseUrl}/Images/update-status.jpg" } } },
                        new AdaptiveCardObject { FilePath = "Resources/AdaptiveCards/Carousel/ChangeHours.json", Data = new Dictionary<string, object> { { "BackgroundImageUrl", $"{this.appSettings.HostBaseUrl}/Images/change-hours.jpg" } } },
                    };

                        var carouselMessage = this.adaptiveCardsService.GetCarouselCollection(carouselCardList);
                        await turnContext.SendActivityAsync(carouselMessage, cancellationToken);
                        return await stepContext.EndDialogAsync();
                    case Commands.SearchEmployee:
                        return await stepContext.BeginDialogAsync(nameof(PersonCardDialog));
                    case Commands.ChangeMyManager:
                        if (!this.appSettings.IsChangeManagerDisabled)
                        {
                            return await stepContext.BeginDialogAsync(nameof(ChangeManagerDialog), new ChangeManagerDialogState { UserAadId = stepContext.Context.Activity.From.AadObjectId });
                        }

                        break;
                }

                await turnContext.SendActivityAsync(MessageFactory.Text("Unsupported command. Please try again."));
                return await stepContext.EndDialogAsync();
            }

            return await stepContext.EndDialogAsync();
        }

        private async Task<DialogTurnResult> InterruptAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            var command = innerDc.Context.Activity.GetSanitizedUserInput();
            if (Commands.SignOutCommands.Contains(command))
            {
                // The bot adapter encapsulates the authentication processes.
                var botAdapter = (BotFrameworkAdapter)innerDc.Context.Adapter;
                await botAdapter.SignOutUserAsync(innerDc.Context, this.connectionName, null, cancellationToken);
                await innerDc.Context.SendActivityAsync(MessageFactory.Text(Strings.UserSignedOutMessage), cancellationToken);
                return await innerDc.CancelAllDialogsAsync();
            }

            return null;
        }

        private async Task<DialogTurnResult> ChildDialogEndStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var result = stepContext.Result as RootDialogResult;
            if (result != null && result.StartAgain)
            {
                var command = stepContext.Context.Activity.GetSanitizedUserInput();

                // If child dialog was cancelled because one of the root commands were called by user, re-start this
                if (Commands.AllRootCommands.Contains(command, StringComparer.OrdinalIgnoreCase))
                {
                    return await stepContext.BeginDialogAsync(nameof(WaterfallDialog));
                }
            }

            return await stepContext.EndDialogAsync();
        }
    }
}
