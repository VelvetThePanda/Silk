using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using Serilog;

namespace Silk.Core.Services.Bot.Music
{
	/// <summary>
	/// A service responsible for music playback in voice channels.
	/// </summary>
	public sealed class MusicVoiceService 
	{
		private readonly DiscordShardedClient _client;
		private readonly ConcurrentDictionary<ulong, VoiceNextConnection> _vcConnections = new();
		private readonly ConcurrentDictionary<ulong, SilkMusicQueue> _queues = new();

		public MusicVoiceService(DiscordShardedClient client)
		{
			_client = client;
		}
		
		public async Task Play(ulong guildId, DiscordChannel channel)
		{
			var queue = EnsureQueueExistsForGuild(guildId);
			
			if (queue.NowPlaying is null || queue.RemainingDuration <= TimeSpan.Zero) 
				queue.Next();
			
			if (queue.NowPlaying is not null)
			{
				if (!_vcConnections.TryGetValue(channel.Guild.Id, out var vnext))
					throw new InvalidOperationException("Join a voice channel first.");

				var wasPaused = queue.IsPaused;
				queue.Resume();
				
				_ = queue.NowPlaying.AudioStream.CopyToAsync(queue.StreamInput, queue.CancellationToken).ConfigureAwait(false);

				if (wasPaused)
					await vnext.ResumeAsync();
				else
					_ = queue.StreamOutput.CopyToAsync(vnext.GetTransmitSink()).ConfigureAwait(false);
			}
		}
		
		public async Task PauseAsync(ulong guildId)
		{
			if (!_vcConnections.TryGetValue(guildId, out _))
				return;
			
			var queue = EnsureQueueExistsForGuild(guildId);
			queue.Pause();
			_vcConnections[guildId].Pause();
		}
		
		[MethodImpl(MethodImplOptions.Synchronized | MethodImplOptions.AggressiveInlining)]
		public void Enqueue(ulong guildId, SilkMusicResult track)
		{
			var queue = EnsureQueueExistsForGuild(guildId);
			queue.Queue.Enqueue(track);
		}
		
		public async Task JoinAsync(DiscordChannel channel)
		{
			var guildId = channel.Guild.Id;
			EnsureQueueExistsForGuild(guildId);
			if (_vcConnections.TryGetValue(guildId, out var vnext))
			{
				if (vnext.TargetChannel == channel)
					return;
				
				vnext.Disconnect();
			}
			_vcConnections[guildId] = await _client.GetShard(guildId).GetVoiceNext().ConnectAsync(channel);
			_vcConnections[guildId].VoiceSocketErrored += async (_, e) => Log.Fatal(e.Exception, "VNext errored!");
			Task.Run(async () =>
			{
				var vn = _vcConnections[guildId];
				while (vn!.IsPlaying) { }
				Log.Warning("VNext stopped playing! Is this intentional?");
			});
			
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private SilkMusicQueue EnsureQueueExistsForGuild(ulong guildId)
		{
			if (!_queues.TryGetValue(guildId, out var queue))
				queue = _queues[guildId] = new();
			return queue;
		}
	}
}