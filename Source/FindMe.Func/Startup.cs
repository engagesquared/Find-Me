// <copyright file="Startup.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

using FindMe.Core.DB;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(FindMe.Func.Startup))]

namespace FindMe.Func
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();
            builder.Services.AddLogging();
            builder.Services.AddDbContext<FindMeDbContext>(p => p.UseSqlServer(FuncConfig.DbConnectionString));
        }
    }
}
