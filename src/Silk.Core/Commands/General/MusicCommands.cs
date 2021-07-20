using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Silk.Core.Services.Bot.Music;
using Silk.Core.Utilities.HelpFormatter;

namespace Silk.Core.Commands.General
{
	[Category(Categories.General)]
	public sealed class MusicCommands : BaseCommandModule
	{
		private readonly MusicSearchService _search;
		private readonly MusicVoiceService _voice;
		public MusicCommands(MusicSearchService search, MusicVoiceService voice)
		{
			_search = search;
			_voice = voice;
		}
		//Here's to hoping that it doesn't all break...
		[Command]
		[RequireGuild]
		//[RequireDJ]
		public async Task SearchYTAsync(CommandContext ctx, string query)
		{
			var results = await _search.SearchYouTubeAsync(query);
			var result = await _search.GetSelectionResultAsync(ctx.Guild, ctx.Channel, ctx.User, results);

			if (result is null)
				await ctx.RespondAsync("You took too long! :(");
			else
				await ctx.RespondAsync($"You picked {result.Video.Title} by {result.Video.Author.Title}");
		}

		[Command]
		[RequireGuild]
		public async Task JoinAsync(CommandContext ctx)
		{
			try
			{
				var valid = await _voice.Join(ctx.Guild.Id, ctx.Member.VoiceState?.Channel);

				await ctx.RespondAsync(valid ? "👍🏽" :"👎🏽");
			}
			catch (InvalidOperationException invOp)
			{
				await ctx.RespondAsync(invOp.Message);
			}
		}
	}
}