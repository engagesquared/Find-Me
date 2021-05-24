// <copyright file="BaseChildDialog.cs" company="Engage Squared">
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
    using FindMe.Bot.Resources;
    using Microsoft.Bot.Builder.Dialogs;

    public class BaseChildDialog : ComponentDialog
    {
        public BaseChildDialog(string id)
            : base(id)
        {
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            var result = await this.InterruptAsync(innerDc, cancellationToken);
            if (result != null)
            {
                return result;
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        private async Task<DialogTurnResult> InterruptAsync(DialogContext dialogContext, CancellationToken cancellationToken = default)
        {
            var sanitizedMessage = dialogContext.Context.Activity.GetSanitizedUserInput();
            if (Commands.AllRootCommands.Contains(sanitizedMessage, StringComparer.OrdinalIgnoreCase))
            {
                return await dialogContext.EndDialogAsync(new RootDialogResult { StartAgain = true });
            }

            if (Commands.CancelCommands.Contains(sanitizedMessage, StringComparer.OrdinalIgnoreCase))
            {
                return await dialogContext.EndDialogAsync();
            }

            return null;
        }
    }
}
