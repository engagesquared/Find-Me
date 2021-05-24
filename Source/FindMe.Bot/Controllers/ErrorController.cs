// <copyright file="ErrorController.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Bot.Controllers
{
    using Microsoft.AspNetCore.Diagnostics;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    [ApiController]
    public class ErrorController : ControllerBase
    {
        public const string Route = "/error";
        private readonly ILogger<ErrorController> logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            this.logger = logger;
        }

        [Route(Route)]
        public IActionResult Error()
        {
            var context = this.HttpContext.Features.Get<IExceptionHandlerFeature>();
            this.logger.LogError(context.Error, "Error");
            return this.Problem(detail: context.Error.Message, title: context.Error.Message);
        }
    }
}
