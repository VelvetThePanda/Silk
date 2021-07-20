using System;
using System.IO;
using DSharpPlus.Entities;

namespace Silk.Core.Services.Bot.Music
{
	public sealed class SilkMusicResult
	{
		public DiscordUser RequestedBy { get; init; }
		public TimeSpan Duration { get; init; }
		
		public Stream AudioStream { get; init; }
	}
}