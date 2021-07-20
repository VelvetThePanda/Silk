using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.Hosting;

namespace Silk.Core.Services.Bot.Music
{
	/// <summary>
	/// A service responsible for music playback in voice channels.
	/// </summary>
	public sealed class MusicVoiceService : IHostedService
	{
		private readonly ConcurrentDictionary<ulong, VoiceNextConnection> _voiceConnections;
		private readonly ConcurrentDictionary<ulong, SemaphoreSlim> _semaphores;
		public async Task StartAsync(CancellationToken cancellationToken) { }
		public async Task StopAsync(CancellationToken cancellationToken) { }

		public async Task Play() { }
		public async Task Stop() { }
		public async Task Pause() { }
		public async Task Repeat() { }
		public async Task Shuffle() { }
		
	}
}