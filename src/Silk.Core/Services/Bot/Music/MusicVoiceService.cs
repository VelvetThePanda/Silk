using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.Hosting;
using Silk.Extensions.DSharpPlus;

namespace Silk.Core.Services.Bot.Music
{
	/// <summary>
	/// A service responsible for music playback in voice channels.
	/// </summary>
	public sealed class MusicVoiceService : IHostedService
	{
		private readonly ConcurrentDictionary<ulong, SilkMusicQueue> _queues;
		public async Task StartAsync(CancellationToken cancellationToken) { }
		public async Task StopAsync(CancellationToken cancellationToken) { }

		public async Task<bool> Join(ulong guildId, DiscordChannel? channel)
		{
			if (channel?.Type is not ChannelType.Voice or ChannelType.Stage)
				throw new InvalidOperationException("Cannot join non-voice-based channel.");

			var semaphore = GetOrCreateSemaphoreForGuild(guildId);
			await semaphore.WaitAsync();
			try
			{
				var vnext = channel.GetClient().GetVoiceNext();

				if (_queues.TryGetValue(guildId, out var vnextConnection) && vnextConnection.Connection?.TargetChannel == channel)
				{
					return false;
				}
				else
				{
					_queues[guildId] = new() {Connection = await vnext.ConnectAsync(channel)};
					return true;
				}
			}
			finally
			{
				semaphore.Release();
			}
		}

		public async Task Play(SilkMusicResult res)
		{
				
		}
		
		public async Task Stop() { }
		public async Task Pause() { }
		public async Task Repeat() { }
		public async Task Shuffle() { }


		private SemaphoreSlim GetOrCreateSemaphoreForGuild(ulong guildId)
		{
			if (!_queues.TryGetValue(guildId, out var queue))
				queue = _queues[guildId] = new();

			return queue.Semaphore;
		}
	}
}