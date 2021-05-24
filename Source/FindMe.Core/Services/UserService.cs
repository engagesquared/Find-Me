// <copyright file="UserService.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Core.Services
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using FindMe.Core.DB;
    using FindMe.Core.DB.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Graph;

    public class UserService
    {
        private readonly GraphService graphService;
        private readonly FindMeDbContext dbContext;

        public UserService(GraphService graphService, FindMeDbContext dbContext)
        {
            this.graphService = graphService;
            this.dbContext = dbContext;
        }

        public async Task<UserEntity> EnsureUser(string aadId, bool includeManager = false)
        {
            var userId = new Guid(aadId);
            var userQuery = this.dbContext.Users.Where(x => x.AadUserId == userId);
            if (includeManager)
            {
                userQuery = userQuery.Include(x => x.Manager);
            }

            var user = userQuery.FirstOrDefault();
            if (user == null)
            {
                var graphUser = await this.graphService.GetUser(aadId, true);
                if (graphUser == null)
                {
                    throw new InvalidOperationException($"User {aadId} doesn't exist in AAD");
                }

                user = new UserEntity
                {
                    AadUserId = userId,
                    Email = graphUser.Mail,
                    EmailNamePart = this.GetEmailNamePart(graphUser.Mail),
                    JobTitle = graphUser.JobTitle,
                    Name = graphUser.DisplayName,
                };
                if (graphUser.Manager is User manager)
                {
                    user.Manager = new UserEntity
                    {
                        AadUserId = new Guid(manager.Id),
                        Email = manager.Mail,
                        EmailNamePart = this.GetEmailNamePart(graphUser.Mail),
                        JobTitle = manager.JobTitle,
                        Name = manager.DisplayName,
                    };
                }
                else
                {
                    user.ManagerIsEmpty = true;
                }

                this.dbContext.Add(user);
                await this.dbContext.SaveChangesAsync();
            }

            if (includeManager && user.Manager == null && !user.ManagerIsEmpty)
            {
                var graphUser = await this.graphService.GetUser(aadId, true);
                if (graphUser.Manager is User manager)
                {
                    user.Manager = new UserEntity
                    {
                        AadUserId = new Guid(manager.Id),
                        Email = manager.Mail,
                        EmailNamePart = this.GetEmailNamePart(graphUser.Mail),
                        JobTitle = manager.JobTitle,
                        Name = manager.DisplayName,
                    };
                }
                else
                {
                    user.ManagerIsEmpty = true;
                }

                await this.dbContext.SaveChangesAsync();
            }

            return user;
        }

        private string GetEmailNamePart(string email)
        {
            var atIndex = email?.IndexOf('@');
            return atIndex > 0 ? email.Substring(0, atIndex.Value) : string.Empty;
        }
    }
}
