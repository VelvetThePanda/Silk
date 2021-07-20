using System;
using DSharpPlus.Entities;

namespace Silk.Core.Services.Bot.Music
{
	public sealed class MusicPlayResult
	{
		public DiscordUser RequestedBy { get; private set; }
		public TimeSpan Duration { get; private set; }
	}
}