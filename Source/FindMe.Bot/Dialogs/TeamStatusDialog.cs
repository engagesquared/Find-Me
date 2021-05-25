// <copyright file="TeamStatusDialog.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Dialogs
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FindMe.Bot.Extensions;
    using FindMe.Bot.Models;
    using FindMe.Bot.Resources;
    using FindMe.Bot.Services;
    using FindMe.Core.DB;
    using FindMe.Core.Services;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.EntityFrameworkCore;

    public class TeamStatusDialog : BaseChildDialog
    {
        private const string TeamStatusesCardPath = "Resources/AdaptiveCards/Status/TeamStatuses.json";

        private readonly AdaptiveCardsService adaptiveCardsService;
        private readonly UserService userService;
        private readonly GraphService graphService;
        private readonly FindMeDbContext dbContext;

        public TeamStatusDialog(FindMeDbContext dbContext, AdaptiveCardsService adaptiveCardsService, GraphService graphService, UserService userService)
            : base(nameof(TeamStatusDialog))
        {
            this.dbContext = dbContext;
            this.adaptiveCardsService = adaptiveCardsService;
            this.graphService = graphService;

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                this.ShowStatusCard,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            this.AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            this.InitialDialogId = nameof(WaterfallDialog);
            this.userService = userService;
        }

        private async Task<DialogTurnResult> ShowStatusCard(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var currentUserId = new Guid(stepContext.Context.Activity.From.AadObjectId);
            var subordinates = this.dbContext.Users.Where(x => x.ManagerId == currentUserId).OrderBy(x => x.Name).ToList();
            if (subordinates.Any())
            {
                var cardData = new TeamStatusesData();
                foreach (var user in subordinates)
                {
                    var activeStatus = await this.userService.GetActiveStatus(user);
                    var teamMemberStatusData = new TeamMemberStatus
                    {
                        ImageUrl = await this.graphService.GetUserPhoto(user.AadUserId.ToString(), Resources.Strings.UnknownAvatarBase64),
                        Name = user.Name,
                        Location = activeStatus?.Location == null ? string.Empty : $"{activeStatus.Location.Address}, tel:{activeStatus.Location.Phone ?? "-"}",
                        Status = activeStatus == null ? "Not set" : $"{activeStatus.Status?.Title ?? activeStatus.OtherStatus} ({activeStatus.Type.GetName()})",
                        Expires = activeStatus?.Expired == null || activeStatus?.Expired == DateTimeOffset.MinValue ? string.Empty : activeStatus.Expired.ToString(Strings.DateTimeFormat),
                    };
                    cardData.Users.Add(teamMemberStatusData);
                }

                var card = this.adaptiveCardsService.CreateAdaptiveCardUsingTemplate(TeamStatusesCardPath, cardData);
                await stepContext.Context.SendActivityAsync(card);
            }
            else
            {
                var card = this.adaptiveCardsService.GetTextCard(Strings.NoSubordinatesMessage);
                await stepContext.Context.SendActivityAsync(card);
            }

            return await stepContext.EndDialogAsync();
        }
    }
}
