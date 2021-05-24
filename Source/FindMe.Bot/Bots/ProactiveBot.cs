// <copyright file="ProactiveBot.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Bots
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FindMe.Bot.Resources;
    using FindMe.Bot.Services;
    using FindMe.Core.DB;
    using FindMe.Core.DB.Entities;
    using FindMe.Core.Services;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Schema;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    public class ProactiveBot
    {
        private readonly AppSettings appSettings;
        private readonly IBotFrameworkHttpAdapter adapter;
        private readonly ILogger<ProactiveBot> logger;
        private readonly StatusCardService statusCardService;
        private readonly AdaptiveCardsService adaptiveCardsService;
        private readonly UpdateHoursDialogService updateHoursDialogService;
        private readonly FindMeDbContext dbContext;

        public ProactiveBot(AppSettings appSettings, IBotFrameworkHttpAdapter adapter, ILogger<ProactiveBot> logger, StatusCardService statusCardService, AdaptiveCardsService adaptiveCardsService, FindMeDbContext dbContext, UpdateHoursDialogService updateHoursDialogService)
        {
            this.appSettings = appSettings;
            this.adapter = adapter;
            this.logger = logger;
            this.statusCardService = statusCardService;
            this.adaptiveCardsService = adaptiveCardsService;
            this.dbContext = dbContext;
            this.updateHoursDialogService = updateHoursDialogService;
        }

        public async Task NotifyUserAboutSchedule(Guid userId)
        {
            try
            {
                var conversationRef = this.dbContext.ConverstaionReferences.Where(x => x.UserId == userId).Include(x => x.User).FirstOrDefault();
                if (conversationRef != null)
                {
                    var message = this.updateHoursDialogService.GetUpdateNotificationCard(Strings.UpdateHoursWeeklyNotificationText);
                    await this.SendMessageAsync(message, conversationRef);
                }
                else
                {
                    this.logger.LogWarning($"Can't find conversation reference for user {userId}");
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Can't sent NotifyUserAboutSchedule notification.");
            }
        }

        public async Task NotifyUserAboutShiftStarted(Guid userId)
        {
            try
            {
                var conversationRef = this.dbContext.ConverstaionReferences.Where(x => x.UserId == userId).Include(x => x.User).FirstOrDefault();
                if (conversationRef != null)
                {
                    var message = this.statusCardService.GetUpdateNotificationCard(Strings.ShiftStartStatusUserNotification);
                    await this.SendMessageAsync(message, conversationRef);
                }
                else
                {
                    this.logger.LogWarning($"Can't find conversation reference for user {userId}");
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Can't sent NotifyUserAboutShiftStarted notification.");
            }
        }

        public async Task NotifyUserAboutStatusExpiration(Guid userId)
        {
            try
            {
                var conversationRef = this.dbContext.ConverstaionReferences.Where(x => x.UserId == userId).Include(x => x.User).FirstOrDefault();
                if (conversationRef != null)
                {
                    var message = this.statusCardService.GetUpdateNotificationCard(Strings.StatusExpiredUserNotification);
                    await this.SendMessageAsync(message, conversationRef);
                }
                else
                {
                    this.logger.LogWarning($"Can't find conversation reference for user {userId}");
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Can't sent NotifyUserAboutStatusExpiration notification.");
            }
        }

        public async Task NotifyUserAboutStatusOverdue(Guid userId)
        {
            try
            {
                var userConversationRef = this.dbContext.ConverstaionReferences.Where(x => x.UserId == userId).Include(x => x.User).FirstOrDefault();
                if (userConversationRef != null)
                {
                    var userMessage = this.statusCardService.GetUpdateNotificationCard(Strings.StatusOverdueUserNotification);
                    await this.SendMessageAsync(userMessage, userConversationRef);

                    var managerConversationRef = this.dbContext.ConverstaionReferences.Where(x => x.UserId == userConversationRef.User.ManagerId).FirstOrDefault();
                    if (managerConversationRef != null)
                    {
                        var managerMessage = this.statusCardService.GetOverdueNotificationCard(userConversationRef.User.Name);
                        await this.SendMessageAsync(managerMessage, managerConversationRef);
                    }
                    else
                    {
                        this.logger.LogWarning($"Can't find conversation reference for user {userId}");
                    }
                }
                else
                {
                    this.logger.LogWarning($"Can't find conversation reference for user {userId}");
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Can't sent NotifyUserAboutStatusOverdue notification.");
            }
        }

        public async Task NotifyUserToUpdateStatus(Guid userId, string senderName)
        {
            try
            {
                var conversationRef = this.dbContext.ConverstaionReferences.Where(x => x.UserId == userId).Include(x => x.User).FirstOrDefault();
                if (conversationRef != null)
                {
                    var message = this.statusCardService.GetUpdateNotificationCard(Strings.StatusRequestMessage.Replace("{name}", senderName));
                    await this.SendMessageAsync(message, conversationRef);
                }
                else
                {
                    this.logger.LogWarning($"Can't find conversation reference for user {userId}");
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Can't sent NotifyUserToUpdateStatus notification.");
            }
        }

        public async Task NotifyUsersManagerChanged(string userId, string newManagerId, string previousManagerId)
        {
            try
            {
                var userConversationRef = this.dbContext.ConverstaionReferences.Where(x => x.UserId == new Guid(userId)).Include(x => x.User).FirstOrDefault();
                var newManagerConversationRef = this.dbContext.ConverstaionReferences.Where(x => x.UserId == new Guid(newManagerId)).Include(x => x.User).FirstOrDefault();
                if (userConversationRef != null)
                {
                    var message = this.adaptiveCardsService.GetTextCard(Strings.UpdateManagerNotificationUserMessage.Replace("{name}", newManagerConversationRef.User.Name));
                    await this.SendMessageAsync(message, userConversationRef);
                }
                else
                {
                    this.logger.LogWarning($"Can't find conversation reference for user {userId}");
                }

                if (newManagerConversationRef != null)
                {
                    var message = this.adaptiveCardsService.GetTextCard(Strings.UpdateManagerNotificationNewManagerMessage.Replace("{name}", userConversationRef.User.Name));
                    await this.SendMessageAsync(message, newManagerConversationRef);
                }
                else
                {
                    this.logger.LogWarning($"Can't find conversation reference for user {newManagerId}");
                }

                if (previousManagerId != null)
                {
                    var previousManagerConversationRef = this.dbContext.ConverstaionReferences.Where(x => x.UserId == new Guid(previousManagerId)).FirstOrDefault();
                    if (previousManagerConversationRef != null)
                    {
                        var message = this.adaptiveCardsService.GetTextCard(Strings.UpdateManagerNotificationPreviousManagerMessage.Replace("{name}", userConversationRef.User.Name));
                        await this.SendMessageAsync(message, previousManagerConversationRef);
                    }
                    else
                    {
                        this.logger.LogWarning($"Can't find conversation reference for user {previousManagerId}");
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Can't sent NotifyUsersManagerChanged notification.");
            }
        }

        private async Task SendMessageAsync(IActivity message, UserConversationReferenceEntity conversationReference)
        {
            var conversation = this.CreateConversationReference(conversationReference);
            async Task Conversationcallback(ITurnContext turnContext, CancellationToken cancellationToken)
            {
                await turnContext.SendActivityAsync(message, cancellationToken);
            }

            await ((BotAdapter)this.adapter).ContinueConversationAsync(this.appSettings.BotAppId, conversation, Conversationcallback, default);
        }

        private ConversationReference CreateConversationReference(UserConversationReferenceEntity conversationReference)
        {
            var serviceUrl = this.dbContext.Config.FirstOrDefault(x => x.Key == CBA.SitMan.Core.Constants.BotServiceUrlConfigKey);
            if (serviceUrl == null)
            {
                throw new InvalidOperationException($"Can't find {CBA.SitMan.Core.Constants.BotServiceUrlConfigKey} config value in DB");
            }

            ConversationReference conversation = new ()
            {
                ChannelId = "msteams",
                Conversation = new ConversationAccount()
                {
                    Id = conversationReference.ConversationId,
                    ConversationType = "personal",
                    TenantId = this.appSettings.TenantId,
                },
                ServiceUrl = serviceUrl.Value,
            };
            return conversation;
        }
    }
}
