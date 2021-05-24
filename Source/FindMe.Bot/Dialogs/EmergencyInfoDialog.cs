// <copyright file="EmergencyInfoDialog.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Dialogs
{
    using System.Threading;
    using System.Threading.Tasks;
    using FindMe.Bot.DialogStates;
    using FindMe.Bot.Services;
    using FindMe.Core.DB;
    using FindMe.Core.Services;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Newtonsoft.Json.Linq;

    public class EmergencyInfoDialog : BaseChildDialog
    {
        private readonly FindMeDbContext dbContext;
        private readonly UserService userService;
        private readonly EmergencyInfoCardsService emergencyInfoCardsService;
        private readonly IStatePropertyAccessor<EmergencyInfoDialogState> stateAccessor;

        public EmergencyInfoDialog(UserService userService, ConversationState conversationState, FindMeDbContext dbContext, EmergencyInfoCardsService emergencyInfoCardsService)
            : base(nameof(EmergencyInfoDialog))
        {
            this.userService = userService;
            this.dbContext = dbContext;
            this.emergencyInfoCardsService = emergencyInfoCardsService;

            this.stateAccessor = conversationState.CreateProperty<EmergencyInfoDialogState>(nameof(EmergencyInfoDialogState));

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                this.ShowEmergencyInfoInitiial,
                this.ShowEditFormOrCancel,
                this.SaveEditFormResult,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            this.AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            this.InitialDialogId = nameof(WaterfallDialog);
        }

        protected override async Task OnEndDialogAsync(ITurnContext context, DialogInstance instance, DialogReason reason, CancellationToken cancellationToken = default)
        {
            var state = await this.stateAccessor.GetAsync(context, () => null);
            if (!string.IsNullOrEmpty(state?.PreviousMessageId))
            {
                var user = await this.userService.EnsureUser(context.Activity.From.AadObjectId);
                var message = this.emergencyInfoCardsService.GetInfoCard(user, true);
                message.Id = state.PreviousMessageId;
                await context.UpdateActivityAsync(message);
            }

            await base.OnEndDialogAsync(context, instance, reason, cancellationToken);
        }

        private async Task<DialogTurnResult> ShowEmergencyInfoInitiial(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await this.stateAccessor.GetAsync(stepContext.Context, () => new EmergencyInfoDialogState());
            var user = await this.userService.EnsureUser(stepContext.Context.Activity.From.AadObjectId);

            var cardMessage = this.emergencyInfoCardsService.GetInfoCard(user);
            await stepContext.Context.SendActivityAsync(cardMessage);
            state.PreviousMessageId = cardMessage.Id;
            await this.stateAccessor.SetAsync(stepContext.Context, state);

            return new DialogTurnResult(DialogTurnStatus.Waiting) { ParentEnded = false };
        }

        private async Task<DialogTurnResult> ShowEditFormOrCancel(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var result = stepContext.Context.Activity.Value as JObject;
            var state = await this.stateAccessor.GetAsync(stepContext.Context, () => null);
            if (state != null && result?.Value<string>("type") == "edit")
            {
                var user = await this.userService.EnsureUser(stepContext.Context.Activity.From.AadObjectId);
                var cardMessage = this.emergencyInfoCardsService.GetEditCard(user);
                cardMessage.Id = state.PreviousMessageId;
                await stepContext.Context.UpdateActivityAsync(cardMessage);
                return new DialogTurnResult(DialogTurnStatus.Waiting) { ParentEnded = false };
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> SaveEditFormResult(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var result = stepContext.Context.Activity.Value as JObject;
            var state = await this.stateAccessor.GetAsync(stepContext.Context, () => null);

            if (state != null && result?.Value<string>("type") == "save")
            {
                var user = await this.userService.EnsureUser(stepContext.Context.Activity.From.AadObjectId);
                this.emergencyInfoCardsService.SetEmergencyInfoResult(result, user);
                await this.dbContext.SaveChangesAsync();
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
