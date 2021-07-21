using DSharpPlus.CommandsNext;
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
	}
}