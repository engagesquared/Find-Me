// <copyright file="ChangeHoursDialog.cs" company="Engage Squared">
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
    using FindMe.Bot.Services;
    using FindMe.Core.DB;
    using FindMe.Core.DB.Entities;
    using FindMe.Core.Services;
    using FindMe.Core.Utils;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Newtonsoft.Json.Linq;

    public class ChangeHoursDialog : BaseChildDialog
    {
        private const string ChooseScheduleUpdateFormatCardPath = "Resources/AdaptiveCards/UpdateHours/ChooseScheduleUpdateFormat.json";
        private const string CommandUseStandardHours = "use standard hours";
        private const string CommandUseShiftPattern = "use shift pattern";
        private const string ActionKey = "action";
        private const string WorkingDaysKey = "workingDays";
        private const string ScheduleTypeKey = "scheduleType";

        private readonly AdaptiveCardsService adaptiveCardsService;
        private readonly GraphService graphService;
        private readonly UserService userService;
        private readonly UpdateHoursDialogService updateHoursDialogService;
        private readonly FindMeDbContext dbContext;
        private readonly IStatePropertyAccessor<ChangeHoursDialogState> stateAccessor;

        public ChangeHoursDialog(ConversationState conversationState, FindMeDbContext dbContext, AdaptiveCardsService adaptiveCardsService, GraphService graphService, UpdateHoursDialogService updateHoursDialogService, UserService userService)
            : base(nameof(ChangeHoursDialog))
        {
            this.dbContext = dbContext;
            this.adaptiveCardsService = adaptiveCardsService;
            this.graphService = graphService;
            this.updateHoursDialogService = updateHoursDialogService;
            this.userService = userService;

            this.stateAccessor = conversationState.CreateProperty<ChangeHoursDialogState>(nameof(ChangeHoursDialogState));

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                this.ShowChooseScheduleUpdateFormatCard,
                this.ShowChooseWorkingDaysCardOrSaveStandardHours,
                this.ShowUpdateHoursCard,
                this.SaveUserScheduleAndCompleteDialog,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            this.AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            this.InitialDialogId = nameof(WaterfallDialog);
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dialogContext, object options, CancellationToken cancellationToken = default)
        {
            if (options is not ChangeHoursDialogState initialState)
            {
                initialState = new ChangeHoursDialogState();
            }

            await this.stateAccessor.SetAsync(dialogContext.Context, initialState);
            return await base.OnBeginDialogAsync(dialogContext, options, cancellationToken);
        }

        private async Task<DialogTurnResult> ShowChooseScheduleUpdateFormatCard(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await this.stateAccessor.GetAsync(stepContext.Context, null);
            if (state != null)
            {
                if (state.ScheduleType.HasValue)
                {
                    return await stepContext.NextAsync(stepContext, cancellationToken);
                }

                var chooseScheduleUpdateFormatCard = this.adaptiveCardsService.CreateAdaptiveCardAttachment(ChooseScheduleUpdateFormatCardPath);
                var chooseScheduleUpdateFormatCardAttachment = MessageFactory.Attachment(chooseScheduleUpdateFormatCard);
                await stepContext.Context.SendActivityAsync(chooseScheduleUpdateFormatCardAttachment);
                state.PreviousMessageId = chooseScheduleUpdateFormatCardAttachment.Id;
                await this.stateAccessor.SetAsync(stepContext.Context, state);
                return new DialogTurnResult(DialogTurnStatus.Waiting) { ParentEnded = false };
            }

            return await stepContext.EndDialogAsync();
        }

        private async Task<DialogTurnResult> ShowChooseWorkingDaysCardOrSaveStandardHours(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var command = (stepContext.Context.Activity.Value as JObject)?[ActionKey]?.ToString();
            var state = await this.stateAccessor.GetAsync(stepContext.Context, null);
            if (state != null && !string.IsNullOrWhiteSpace(command))
            {
                if (command == CommandUseStandardHours)
                {
                    state.ScheduleType = UserScheduleType.Standard;
                }
                else if (command == CommandUseShiftPattern)
                {
                    state.ScheduleType = UserScheduleType.Shift;
                }
                else
                {
                    // Unrecognized command, end dialog.
                    return await stepContext.EndDialogAsync();
                }

                var userId = new Guid(stepContext.Context.Activity.From.AadObjectId);
                var userSchedule = this.dbContext.WeekSchedules.Where(x => x.UserId == userId).OrderByDescending(x => x.Id).FirstOrDefault();
                var weeklySchedule = new List<DaySchedule>();
                var localStartOfTheWeek = DateTimeUtils.GetStartOfTheWeek(stepContext.Context.Activity.LocalTimestamp);
                if (userSchedule != null)
                {
                    weeklySchedule = this.updateHoursDialogService.GetWeekSchedule(userSchedule, localStartOfTheWeek);
                }
                else
                {
                    var graphWorkingHours = await this.graphService.GetUserWorkingTime(stepContext.Context.Activity.From.AadObjectId);
                    weeklySchedule = this.updateHoursDialogService.GetWeekSchedule(graphWorkingHours, localStartOfTheWeek);
                }

                state.Schedule = weeklySchedule;
                var message = this.updateHoursDialogService.GetWorkingDaysCard(weeklySchedule);

                if (state.PreviousMessageId != null)
                {
                    message.Id = state.PreviousMessageId;
                    await stepContext.Context.UpdateActivityAsync(message);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(message);
                    state.PreviousMessageId = message.Id;
                }

                await this.stateAccessor.SetAsync(stepContext.Context, state);
                return new DialogTurnResult(DialogTurnStatus.Waiting) { ParentEnded = false };
            }

            return await stepContext.EndDialogAsync();
        }

        private async Task<DialogTurnResult> ShowUpdateHoursCard(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await this.stateAccessor.GetAsync(stepContext.Context, null);
            if (state != null)
            {
                var result = stepContext.Context.Activity.Value as JObject;
                var workingDaysString = result[WorkingDaysKey]?.ToObject<string>();
                var workingDays = string.IsNullOrEmpty(workingDaysString)
                    ? new List<DayOfWeek>()
                    : workingDaysString.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => (DayOfWeek)Convert.ToInt32(x)).ToList();
                if (workingDays.Count > 0)
                {
                    var daysToShow = state.Schedule.Where(x => workingDays.Contains(x.DayOfWeek)).ToList();
                    var daysToAdd = workingDays.Where(x => daysToShow.All(ds => ds.DayOfWeek != x)).Select(x => new DaySchedule { DayOfWeek = x }).ToList();
                    daysToShow.AddRange(daysToAdd);
                    daysToShow = daysToShow.OrderBy(x => x.DayOfWeek == DayOfWeek.Sunday ? 100 : (int)x.DayOfWeek).ToList();

                    var changeHoursCard = this.updateHoursDialogService.GetWorkingHoursCard(daysToShow);
                    changeHoursCard.Id = state.PreviousMessageId;
                    await stepContext.Context.UpdateActivityAsync(changeHoursCard);
                    return new DialogTurnResult(DialogTurnStatus.Waiting) { ParentEnded = false };
                }
                else
                {
                    var validationCard = this.adaptiveCardsService.GetTextCard(Resources.Strings.UpdateHoursErrorMessage, true);
                    validationCard.Id = state.PreviousMessageId;
                    await stepContext.Context.UpdateActivityAsync(validationCard);
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                }
            }

            return await stepContext.EndDialogAsync();
        }

        private async Task<DialogTurnResult> SaveUserScheduleAndCompleteDialog(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await this.stateAccessor.GetAsync(stepContext.Context, null);
            if (state != null && stepContext.Context.Activity.Value is JObject cardResult)
            {
                if (state.ScheduleType == null)
                {
                    var validationCard = this.adaptiveCardsService.GetTextCard(Resources.Strings.UpdateHoursErrorMessage, true);
                    validationCard.Id = state.PreviousMessageId;
                    await stepContext.Context.UpdateActivityAsync(validationCard);
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                }

                var user = await this.userService.EnsureUser(stepContext.Context.Activity.From.AadObjectId);
                var startOfTheWeekUtc = DateTimeUtils.GetStartOfTheWeekUtc();
                var startOfTheWeekUser = DateTimeUtils.GetStartOfTheWeek(stepContext.Context.Activity.LocalTimestamp);
                var currentWeekSchedule = this.dbContext.WeekSchedules.FirstOrDefault(x => x.UserId == user.AadUserId && x.StartDateUtc == startOfTheWeekUtc);
                if (currentWeekSchedule == null)
                {
                    currentWeekSchedule = new WeekScheduleEntity
                    {
                        StartDateUtc = startOfTheWeekUtc,
                        User = user,
                    };
                    await this.dbContext.WeekSchedules.AddAsync(currentWeekSchedule);
                }

                currentWeekSchedule.StartDate = startOfTheWeekUser;
                currentWeekSchedule.ScheduleType = state.ScheduleType.Value;

                this.updateHoursDialogService.UpdateScheduleHours(currentWeekSchedule, cardResult);

                if (currentWeekSchedule.ScheduleType != user.UserScheduleType)
                {
                    user.UserScheduleType = currentWeekSchedule.ScheduleType;
                }

                await this.dbContext.SaveChangesAsync();

                var summaryCard = this.updateHoursDialogService.SetupChangeHoursSummaryCard(currentWeekSchedule);
                summaryCard.Id = state.PreviousMessageId;
                await stepContext.Context.UpdateActivityAsync(summaryCard);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
