// <copyright file="AppSettings.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot
{
    using Microsoft.Extensions.Configuration;

    public class AppSettings
    {
        private readonly IConfiguration configuration;

        public AppSettings(IConfiguration configuration)
        {
            this.configuration = configuration;
            this.Setup();
        }

        public string BotAppId { get; private set; }

        public string BotAppPassword { get; private set; }

        public string AadUserAppConnectionName { get; private set; }

        public string AadAppId { get; private set; }

        public string AadAppPassword { get; private set; }

        public string TenantId { get; private set; }

        public string HostBaseUrl { get; private set; }

        private void Setup()
        {
            this.BotAppId = this.configuration["MicrosoftAppId"];
            this.BotAppPassword = this.configuration["MicrosoftAppPassword"];
            this.TenantId = this.configuration["TenantId"];
            this.AadUserAppConnectionName = this.configuration["FMAppConnectionName"];
            this.AadAppId = this.configuration["FMAppId"];
            this.AadAppPassword = this.configuration["FMAppPassword"];
            this.HostBaseUrl = this.configuration["HostBaseUrl"];
        }
    }
}
