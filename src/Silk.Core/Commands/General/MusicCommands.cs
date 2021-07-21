using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.VoiceNext;
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
		//[RequireMusicGuild]
		public async Task Play(CommandContext ctx, string path)
		{
			var vnext = ctx.Client.GetVoiceNext();
			var connect = await vnext.ConnectAsync(ctx.Member.VoiceState.Channel);
			var file = 
		}

		[Command]
		[RequireGuild]
		public async Task JoinAsync(CommandContext ctx)
		{
			var message = await _voice.JoinAsync(ctx.Guild.Id, ctx.Member.VoiceState?.Channel);

			await ctx.RespondAsync(message);
		}
	}
}