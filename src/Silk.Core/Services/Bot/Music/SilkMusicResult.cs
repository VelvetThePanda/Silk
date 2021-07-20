using System;
using DSharpPlus.Entities;
using YoutubeExplode.Search;

namespace Silk.Core.Services.Bot.Music
{
	public sealed class SilkMusicResult
	{
		public DiscordUser RequestedBy { get; init; }
		public TimeSpan Duration { get; init; }
		
		public VideoSearchResult Video { get; init; }
	}
}