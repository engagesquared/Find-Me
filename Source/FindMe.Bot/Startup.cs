// <copyright file="Startup.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot
{
    using FindMe.Bot.Adapters;
    using FindMe.Bot.Bots;
    using FindMe.Bot.Controllers;
    using FindMe.Bot.Dialogs;
    using FindMe.Bot.Services;
    using FindMe.Core.DB;
    using FindMe.Core.Services;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.ApplicationInsights;
    using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();
            services.AddHttpClient();
            services.AddSingleton<AppSettings>();

            // Create the Bot Framework Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            this.AddApplicationInsights(services);

            // Create the storage we'll be using for User and Conversation state.
            services.AddSingleton<IStorage, MemoryStorage>();

            // Create the User state. (Used in this bot's Dialog implementation.)
            services.AddSingleton<UserState>();

            // Create the Conversation state. (Used by the Dialog system itself.)
            services.AddSingleton<ConversationState>();

            // The Dialogs that will be run by the bot.
            services.AddScoped<RootDialog>();
            services.AddScoped<EmergencyInfoDialog>();
            services.AddScoped<StatusDialog>();
            services.AddScoped<ChangeHoursDialog>();
            services.AddScoped<TeamReportDialog>();
            services.AddScoped<SearchEmployeeDialog>();
            services.AddScoped<PersonCardDialog>();
            services.AddScoped<ChangeManagerDialog>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<FindMeBot>();
            services.AddTransient<ProactiveBot>();

            services.AddScoped<GraphService>(
                (services) =>
                {
                    var appSettings = services.GetService<AppSettings>();
                    return new GraphService(appSettings.AadAppId, appSettings.TenantId, appSettings.AadAppPassword);
                });

            services.AddDbContext<FindMeDbContext>(p => p.UseSqlServer(this.Configuration.GetConnectionString("FindMeDb")));
            services.AddScoped<UserService>();

            services.AddTransient<AdaptiveCardsService>();
            services.AddSingleton<EmergencyInfoCardsService>();
            services.AddSingleton<TeamReportDialogService>();
            services.AddTransient<UpdateHoursDialogService>();
            services.AddSingleton<StatusCardService>();
            services.AddTransient<PersonCardService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseExceptionHandler(ErrorController.Route);
            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

            // app.UseHttpsRedirection();
        }

        private void AddApplicationInsights(IServiceCollection services)
        {
            // Add Application Insights services into service collection
            services.AddApplicationInsightsTelemetry();

            // Create the telemetry client.
            services.AddSingleton<IBotTelemetryClient, BotTelemetryClient>();

            // Add telemetry initializer that will set the correlation context for all telemetry items.
            services.AddSingleton<ITelemetryInitializer, OperationCorrelationTelemetryInitializer>();

            // Add telemetry initializer that sets the user ID and session ID (in addition to other bot-specific properties such as activity ID)
            services.AddSingleton<ITelemetryInitializer, TelemetryBotIdInitializer>();

            // Create the telemetry middleware to initialize telemetry gathering
            services.AddSingleton<TelemetryInitializerMiddleware>();

            // Create the telemetry middleware (used by the telemetry initializer) to track conversation events
            services.AddSingleton<TelemetryLoggerMiddleware>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    }
}
