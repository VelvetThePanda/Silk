using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Humanizer;
using MediatR;
using Silk.Core.Data.MediatR.Infractions;
using Silk.Core.Data.Models;
using Silk.Core.Services.Interfaces;
using Silk.Core.Types;
using Silk.Core.Utilities;
using Silk.Core.Utilities.HelpFormatter;

namespace Silk.Core.Commands.Moderation
{
	[Category(Categories.Mod)]
	public class StrikeCommand : BaseCommandModule
	{
		private readonly IInfractionService _infractionHelper;
		private readonly IMediator _mediator;
		public StrikeCommand(IInfractionService infractionHelper, IMediator mediator)
		{
			_infractionHelper = infractionHelper;
			_mediator = mediator;
		}

		[Command("strike")]
		[RequireFlag(UserFlag.Staff)]
		[Aliases("warn", "w", "bonk")]
		[Description("Strike a user and add it to their moderation history.")]
		public async Task Strike(CommandContext ctx, DiscordUser user, [RemainingText] string reason = "Not Given.")
		{
			
			var escalated = await CheckForEscalationAsync(ctx, user, reason);
			var result = await _infractionHelper.StrikeAsync(user.Id, ctx.Guild.Id, ctx.User.Id, reason, escalated.Item1);
			var response = "";
			
			if (escalated.Item1)
			{
				response = result switch
				{
					InfractionResult.SucceededWithNotification => $"Successfully {escalated.Item2} (Notified with Direct Message)",
					InfractionResult.SucceededWithoutNotification => $"Successfully {escalated.Item2} user (Failed to Direct Message)",
					InfractionResult.FailedLogPermissions => $"Successfully {escalated.Item2} user (Failed to Log).",
					_ => $"I attempted to {escalated.Item2} {user.Username} but the response was: `{result.Humanize(LetterCasing.Title)}`. \nThis is probably safe to ignore!"
				};
			}
			else
			{
				response = result switch
				{
					InfractionResult.SucceededWithNotification => $"Successfully warned user (Notified with Direct Message)",
					InfractionResult.SucceededWithoutNotification => $"Successfully warned user (Failed to Direct Message)",
					InfractionResult.FailedLogPermissions => $"Successfully warned user (Failed to Log).",
					_ => $"I attempted to warn {user.Username} but the response was: `{result.Humanize(LetterCasing.Title)}`. \nThis is probably safe to ignore!"
				};
			}
			
			await ctx.RespondAsync(response);
		}
		
		private async Task<(bool, InfractionType)> CheckForEscalationAsync(CommandContext ctx, DiscordUser user, string reason)
		{
			var infractions = await _mediator.Send(new GetUserInfractionsRequest(ctx.Guild.Id, user.Id));
			var interactivity = ctx.Client.GetInteractivity();
			
			if (infractions.Count(inf => inf.Type != InfractionType.Note) < 6)
				return (false, default);

			var currentStep = await _infractionHelper.GetCurrentInfractionStepAsync(ctx.Guild.Id, infractions);
			var currentStepType = currentStep.Type is InfractionType.Ignore ? InfractionType.Ban : currentStep.Type;
			var builder = new DiscordMessageBuilder()
				.WithContent("User has 5 or more infractions on record. Would you like to escalate?")
				.AddComponents(
					new DiscordButtonComponent(ButtonStyle.Success, $"escalate_{ctx.Message.Id}", $"Escalate to {currentStepType.Humanize(LetterCasing.Sentence)}", emoji: new(834860005685198938)),
					new DiscordButtonComponent(ButtonStyle.Danger, $"do_not_escalate_{ctx.Message.Id}", "Do not escalate", emoji: new(834860005584666644)));

			var msg = await ctx.RespondAsync(builder);
			var res = await interactivity.WaitForButtonAsync(msg, ctx.User, token: null);

			if (res.TimedOut)
				return (false, default);

			var escalated = res.Result.Id.StartsWith("escalate");
			await res.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new() {Content = escalated ? "Got it." : "Proceeding with strike."});
			_ = TimedDelete();
			return (escalated, currentStepType);

			async Task TimedDelete()
			{
				await Task.Delay(3000);
				await msg!.DeleteAsync();
			}
		}
	}
}