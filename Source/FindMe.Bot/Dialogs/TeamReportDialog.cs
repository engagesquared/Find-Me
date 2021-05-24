// <copyright file="TeamReportDialog.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Dialogs
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using FindMe.Bot.DialogStates;
    using FindMe.Bot.Resources;
    using FindMe.Bot.Services;
    using FindMe.Core.DB;
    using FindMe.Core.Services;
    using FindMe.Core.Utils;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Microsoft.EntityFrameworkCore;

    public class TeamReportDialog : ComponentDialog
    {
        private readonly FindMeDbContext dbContext;
        private readonly UserService userService;
        private readonly IStatePropertyAccessor<TeamReportDialogState> stateAccessor;
        private readonly IHttpClientFactory clientFactory;
        private readonly AdaptiveCardsService adaptiveCardsService;
        private readonly TeamReportDialogService teamReportDialogService;

        public TeamReportDialog(ConversationState conversationState, IHttpClientFactory clientFactory, AdaptiveCardsService adaptiveCardsService, FindMeDbContext dbContext, UserService userService, TeamReportDialogService teamReportDialogService)
            : base(nameof(TeamReportDialog))
        {
            this.stateAccessor = conversationState.CreateProperty<TeamReportDialogState>(nameof(TeamReportDialogState));

            var waterfallSteps = new WaterfallStep[]
            {
                this.RequestFileUploadStep,
            };

            this.AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            this.InitialDialogId = nameof(WaterfallDialog);
            this.clientFactory = clientFactory;
            this.adaptiveCardsService = adaptiveCardsService;
            this.dbContext = dbContext;
            this.userService = userService;
            this.teamReportDialogService = teamReportDialogService;
        }

        public async Task<bool> ShouldRunFileProcessingAsync(ITurnContext<IInvokeActivity> turnContext)
        {
            var state = await this.stateAccessor.GetAsync(turnContext, () => null);
            if (state != null)
            {
                return true;
            }

            return false;
        }

        public async Task ProcessFileAcceptedAsync(ITurnContext<IInvokeActivity> turnContext, FileConsentCardResponse fileConsentCardResponse, CancellationToken cancellationToken)
        {
            await this.ReportFileAcceptedStep(turnContext, fileConsentCardResponse, cancellationToken);
        }

        public async Task ProcessFileDeclineAsync(ITurnContext<IInvokeActivity> turnContext, FileConsentCardResponse fileConsentCardResponse, CancellationToken cancellationToken)
        {
            await this.ReportFileDeclinedStep(turnContext, fileConsentCardResponse, cancellationToken);
        }

        private async Task<DialogTurnResult> RequestFileUploadStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await this.stateAccessor.GetAsync(stepContext.Context, () => new TeamReportDialogState());

            var currentUser = await this.userService.EnsureUser(stepContext.Context.Activity.From.AadObjectId);
            var lastWeekStart = DateTimeUtils.GetStartOfTheWeekUtc().AddDays(-7);
            var thisWeekStart = DateTimeUtils.GetStartOfTheWeekUtc();

            var weekSchedules = this.dbContext.WeekSchedules.Where(x => x.StartDateUtc == lastWeekStart && x.User.ManagerId == currentUser.AadUserId)
                .Include(x => x.User).ToList().OrderBy(x => x.User.Name).ToList();
            var userStatuses = this.dbContext.UserStatuses.Where(x => x.Created > lastWeekStart && x.Created < thisWeekStart && x.User.ManagerId == currentUser.AadUserId)
                .Include(x => x.User).Include(x => x.Location).Include(x => x.Status).Include(x => x.CreatedBy).ToList()
                .OrderBy(x => x.User.Name).ThenBy(x => x.Created).ToList();

            string filename = $"Report_{DateTime.Now.ToString("yyyyMMdd_HHmm")}.csv";
            var file = this.teamReportDialogService.GetCsvReport(weekSchedules, userStatuses);

            var fileCard = new FileConsentCard
            {
                Description = Strings.ReportConsentDescription,
                SizeInBytes = file.Length,
                AcceptContext = new { filename = filename },
            };
            var consentRequestAttachment = new Attachment
            {
                Content = fileCard,
                ContentType = FileConsentCard.ContentType,
                Name = filename,
            };

            var message = MessageFactory.Attachment(consentRequestAttachment);
            await stepContext.Context.SendActivityAsync(message, cancellationToken);
            state.PreviousMessageId = message.Id;
            state.ReportContent = file;

            await this.stateAccessor.SetAsync(stepContext.Context, state);
            return await stepContext.EndDialogAsync();
        }

        private async Task ReportFileAcceptedStep(ITurnContext<IInvokeActivity> turnContext, FileConsentCardResponse fileConsentCardResponse, CancellationToken cancellationToken)
        {
            var state = await this.stateAccessor.GetAsync(turnContext, () => null);
            if (state != null)
            {
                // clean state
                await this.stateAccessor.SetAsync(turnContext, null);
                var client = this.clientFactory.CreateClient();
                using (var fileStream = new MemoryStream(state.ReportContent))
                {
                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentLength = state.ReportContent.Length;
                    fileContent.Headers.ContentRange = new ContentRangeHeaderValue(0, state.ReportContent.Length - 1, state.ReportContent.Length);
                    await client.PutAsync(fileConsentCardResponse.UploadInfo.UploadUrl, fileContent, cancellationToken);
                }

                var downloadCard = new FileInfoCard
                {
                    UniqueId = fileConsentCardResponse.UploadInfo.UniqueId,
                    FileType = fileConsentCardResponse.UploadInfo.FileType,
                };

                var asAttachment = new Attachment
                {
                    Content = downloadCard,
                    ContentType = FileInfoCard.ContentType,
                    Name = fileConsentCardResponse.UploadInfo.Name,
                    ContentUrl = fileConsentCardResponse.UploadInfo.ContentUrl,
                };

                var message = this.adaptiveCardsService.GetTextCard(Strings.ReportIsReadyMessageText);
                if (!string.IsNullOrEmpty(state.PreviousMessageId))
                {
                    message.Id = state.PreviousMessageId;
                    await turnContext.UpdateActivityAsync(message);
                }
                else
                {
                    await turnContext.SendActivityAsync(message, cancellationToken);
                }

                await turnContext.SendActivityAsync(MessageFactory.Attachment(asAttachment), cancellationToken);
            }
        }

        private async Task ReportFileDeclinedStep(ITurnContext<IInvokeActivity> turnContext, FileConsentCardResponse fileConsentCardResponse, CancellationToken cancellationToken)
        {
            var state = await this.stateAccessor.GetAsync(turnContext, () => null);
            if (state != null)
            {
                // clean state
                await this.stateAccessor.SetAsync(turnContext, null);
            }

            var message = this.adaptiveCardsService.GetTextCard(Strings.ReportWasDeclinedMessageText);
            if (!string.IsNullOrEmpty(state?.PreviousMessageId))
            {
                message.Id = state.PreviousMessageId;
                await turnContext.UpdateActivityAsync(message);
            }
            else
            {
                await turnContext.SendActivityAsync(message, cancellationToken);
            }
        }
    }
}
