using System;
using System.Collections.Concurrent;

namespace Silk.Core.Services.Bot.Music
{
	public sealed class SilkMusicQueue
	{
		public SilkMusicResult? NowPlaying => _nowPlaying;
		private SilkMusicResult? _nowPlaying;
		
		public ConcurrentQueue<SilkMusicResult> Queue { get; } = new();

		public TimeSpan RemainingDuration => _nowPlaying is null ? TimeSpan.Zero : 
			TimeSpan.FromMilliseconds(_nowPlaying.Duration.TotalMilliseconds - _nowPlaying.AudioStream.Length /*bytes*/ / _nowPlaying.Duration.TotalMilliseconds * _nowPlaying.AudioStream.Position);

		public void Next() => Queue.TryDequeue(out _nowPlaying);
	}
}