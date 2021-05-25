// <copyright file="StatusDialog.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Dialogs
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FindMe.Bot.DialogStates;
    using FindMe.Bot.Extensions;
    using FindMe.Bot.Services;
    using FindMe.Core.DB;
    using FindMe.Core.DB.Entities;
    using FindMe.Core.Services;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json.Linq;

    public class StatusDialog : BaseChildDialog
    {
        private const string UpdateStatusCommand = "update status command";
        private const string PersonCardUpdateStatusCommand = "person card update status";
        private const string ExtendStatusCommand = "extend status command";
        private readonly FindMeDbContext dbContext;
        private readonly UserService userService;
        private readonly AdaptiveCardsService adaptiveCardsService;
        private readonly StatusCardService statusCardService;
        private readonly GraphService graphService;
        private readonly IStatePropertyAccessor<StatusDialogState> stateAccessor;

        public StatusDialog(UserService userService, ConversationState conversationState, FindMeDbContext dbContext, StatusCardService statusCardService, AdaptiveCardsService adaptiveCardsService, GraphService graphService)
            : base(nameof(StatusDialog))
        {
            this.userService = userService;
            this.dbContext = dbContext;
            this.statusCardService = statusCardService;
            this.adaptiveCardsService = adaptiveCardsService;
            this.graphService = graphService;

            this.stateAccessor = conversationState.CreateProperty<StatusDialogState>(nameof(StatusDialogState));

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                this.ShowStatusViewForm,
                this.HandleUserCommand,
                this.HandleCommandResult,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            this.AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            this.InitialDialogId = nameof(WaterfallDialog);
        }

        protected override async Task OnEndDialogAsync(ITurnContext context, DialogInstance instance, DialogReason reason, CancellationToken cancellationToken = default)
        {
            var state = await this.stateAccessor.GetAsync(context, () => null);

            if (state?.PreviousMessageId != null)
            {
                var user = await this.userService.EnsureUser(state.UserIdToUpdate);
                var activeStatus = await this.userService.GetActiveStatus(user);

                var cardMessage = this.statusCardService.GetViewCard(user.Name, state.CanSeeSensitiveInfo, false, activeStatus, null, true);
                cardMessage.Id = state.PreviousMessageId;
                await context.UpdateActivityAsync(cardMessage);
            }

            await this.stateAccessor.SetAsync(context, null);
            await base.OnEndDialogAsync(context, instance, reason, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dialogContext, object options, CancellationToken cancellationToken = default)
        {
            // Build step for current user
            if (options is not StatusDialogState state)
            {
                state = new StatusDialogState
                {
                    CanSeeSensitiveInfo = true,
                    UserIdToUpdate = dialogContext.Context.Activity.From.AadObjectId,
                    PreviousMessageId = null,
                };
            }

            await this.stateAccessor.SetAsync(dialogContext.Context, state);
            return await base.OnBeginDialogAsync(dialogContext, options, cancellationToken);
        }

        private async Task<DialogTurnResult> ShowStatusViewForm(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await this.stateAccessor.GetAsync(stepContext.Context, () => null);

            if (state != null)
            {
                if (state.ShowStatusEditForm == true)
                {
                    state.ChosenCommand = UpdateStatusCommand;
                    await this.stateAccessor.SetAsync(stepContext.Context, state);
                    return await stepContext.NextAsync();
                }

                var user = await this.userService.EnsureUser(state.UserIdToUpdate);
                var latestStatuses = this.dbContext.UserStatuses.Where(x => x.UserId == user.AadUserId).Include(x => x.CreatedBy).OrderByDescending(x => x.Created).Take(6).ToList();
                var activeStatus = this.dbContext.UserStatuses.Where(x => x.UserId == user.AadUserId && x.Expired > DateTimeOffset.Now)
                    .Include(x => x.CreatedBy).Include(x => x.Location).Include(x => x.Status)
                    .OrderByDescending(x => x.Created).FirstOrDefault();

                var extendStatusIsShown = false;
                if (activeStatus != null)
                {
                    state.ActiveStatusId = activeStatus.Id;
                    var oneHourBeforeExpiration = activeStatus.Expired.AddHours(-1);
                    var statusWillExpireInOneHour = DateTimeOffset.Compare(oneHourBeforeExpiration, DateTimeOffset.Now) < 0 && DateTimeOffset.Compare(DateTimeOffset.Now, activeStatus.Expired) < 0;
                    var invokerId = stepContext.Context.Activity.From.AadObjectId;
                    var managerId = user.ManagerId?.ToString() ?? string.Empty;
                    extendStatusIsShown = statusWillExpireInOneHour && (invokerId == user.AadUserId.ToString() || invokerId == managerId);
                }
                else if (user.UserScheduleType == UserScheduleType.Standard || user.UserScheduleType == null)
                {
                    activeStatus = await this.graphService.GetUserCurrentStatus(state.UserIdToUpdate);
                }

                var cardMessage = this.statusCardService.GetViewCard(user.Name, state.CanSeeSensitiveInfo, false, activeStatus, latestStatuses, false, extendStatusIsShown);
                await stepContext.Context.SendActivityAsync(cardMessage);

                state.PreviousMessageId = cardMessage.Id;
                await this.stateAccessor.SetAsync(stepContext.Context, state);

                return new DialogTurnResult(DialogTurnStatus.Waiting) { ParentEnded = false };
            }

            return await stepContext.EndDialogAsync();
        }

        private async Task<DialogTurnResult> HandleUserCommand(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await this.stateAccessor.GetAsync(stepContext.Context, null);
            if (state == null)
            {
                return await stepContext.EndDialogAsync();
            }

            var command = stepContext.Context.Activity.GetSanitizedUserInput();
            state.ChosenCommand = command;
            await this.stateAccessor.SetAsync(stepContext.Context, state);

            switch (command)
            {
                case UpdateStatusCommand:
                case PersonCardUpdateStatusCommand:
                    var user = await this.userService.EnsureUser(state.UserIdToUpdate);
                    var statuses = this.dbContext.Statuses.OrderBy(x => x.Order).ToList();

                    IQueryable<UserStatusEntity> locationsQuery;
                    if (state.CanSeeSensitiveInfo)
                    {
                        locationsQuery = this.dbContext.UserStatuses.Where(x => x.UserId == user.AadUserId);
                    }
                    else
                    {
                        locationsQuery = this.dbContext.UserStatuses.Where(x => x.UserId == user.AadUserId && x.IsSensitive == false);
                    }

                    var locations = locationsQuery.OrderByDescending(x => x.Created).Select(x => x.Location).Distinct().Take(5).ToList();
                    var cardMessage = this.statusCardService.GetEditCard(user, statuses, locations);
                    if (state.PreviousMessageId != null)
                    {
                        cardMessage.Id = state.PreviousMessageId;
                        await stepContext.Context.UpdateActivityAsync(cardMessage);
                    }
                    else
                    {
                        await stepContext.Context.SendActivityAsync(cardMessage);
                        state.PreviousMessageId = cardMessage.Id;
                        await this.stateAccessor.SetAsync(stepContext.Context, state);
                    }

                    return new DialogTurnResult(DialogTurnStatus.Waiting) { ParentEnded = false };
                case ExtendStatusCommand:
                    return await stepContext.NextAsync();
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
                case PersonCardUpdateStatusCommand:
                    var result = stepContext.Context.Activity.Value as JObject;
                    if (result?.Value<string>("type") == "save")
                    {
                        var errors = this.statusCardService.GetValidationErrors(result);
                        if (errors.Any())
                        {
                            var message = this.adaptiveCardsService.GetTextCard($"Validation error: {string.Join("; ", errors)}", true);
                            message.Id = state.PreviousMessageId;
                            await stepContext.Context.UpdateActivityAsync(message);
                            await this.stateAccessor.DeleteAsync(stepContext.Context);
                        }
                        else
                        {
                            var user = await this.userService.EnsureUser(state.UserIdToUpdate);
                            var author = await this.userService.EnsureUser(stepContext.Context.Activity.From.AadObjectId);
                            var newUserStatus = new UserStatusEntity();
                            this.statusCardService.SetUserStatus(result, newUserStatus, stepContext.Context.Activity.LocalTimestamp);
                            newUserStatus.CreatedBy = author;
                            newUserStatus.User = user;
                            newUserStatus.Created = stepContext.Context.Activity.LocalTimestamp ?? DateTimeOffset.Now;
                            this.dbContext.Add(newUserStatus);
                            await this.dbContext.SaveChangesAsync();
                        }
                    }

                    break;
                case ExtendStatusCommand:
                    var activeStatus = this.dbContext.UserStatuses.Where(x => x.Id == state.ActiveStatusId).FirstOrDefault();
                    if (activeStatus == null)
                    {
                        break;
                    }

                    var extendedUserStatus = new UserStatusEntity();
                    extendedUserStatus.UserId = activeStatus.UserId;
                    extendedUserStatus.Type = activeStatus.Type;
                    extendedUserStatus.StatusId = activeStatus.StatusId;
                    extendedUserStatus.OtherStatus = activeStatus.OtherStatus;
                    extendedUserStatus.LocationId = activeStatus.LocationId;
                    extendedUserStatus.IsSensitive = activeStatus.IsSensitive;
                    extendedUserStatus.Comments = activeStatus.Comments;
                    extendedUserStatus.CreatedById = new Guid(stepContext.Context.Activity.From.AadObjectId);
                    extendedUserStatus.Expired = activeStatus.Expired.AddHours(1);
                    extendedUserStatus.Created = stepContext.Context.Activity.LocalTimestamp ?? DateTimeOffset.Now;
                    this.dbContext.Add(extendedUserStatus);
                    await this.dbContext.SaveChangesAsync();
                    break;
                default:
                    break;
            }

            return await stepContext.EndDialogAsync();
        }
    }
}
