using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Silk.Core.Services.Bot.Music;
using Silk.Core.Utilities.HelpFormatter;
using YoutubeExplode.Videos;

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

		[Command]
		[RequireGuild]
		public async Task Play(CommandContext ctx, string url)
		{
			if (VideoId.TryParse(url) is null)
			{
				await ctx.RespondAsync("That's not a youtube video :(");
				return;
			}

			var rest = await _search.GetVideoFromLinkAsync(url, ctx.User);
			
			_voice.Enqueue(ctx.Guild.Id, rest);

			await _voice.JoinAsync(ctx.Member.VoiceState.Channel);
			await _voice.Play(ctx.Guild.Id, ctx.Channel);
		}

		[Command]
		public Task Pause(CommandContext ctx) => _voice.PauseAsync(ctx.Guild.Id);

		[Command]
		public Task Resume(CommandContext ctx) => _voice.Play(ctx.Guild.Id, ctx.Member.VoiceState.Channel);
	}
}