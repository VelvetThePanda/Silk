using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.VoiceNext;

namespace Silk.Core.Services.Bot.Music
{
	public sealed class SilkMusicQueue
	{
		public VoiceNextConnection? Connection 
		{ 
			get => _connection;
			init => _connection = value ?? throw new ArgumentNullException(nameof(value));
		}
		
		private readonly VoiceNextConnection? _connection;

		public SilkMusicResult? NowPlaying => _nowPlaying;
		private SilkMusicResult? _nowPlaying;

		public SemaphoreSlim Semaphore { get; } = new(1);
		private readonly ConcurrentQueue<SilkMusicResult> _queue = new();

		private Process _ffmpeg;


		private void StartFFMPeg()
		{
			_ffmpeg?.Kill();
			_ffmpeg = Process.Start(new ProcessStartInfo()
			{
				FileName = "./ffmpeg.exe",
				RedirectStandardOutput = true,
				RedirectStandardInput = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				Arguments = "-i - -f mp3 -ac 2 -f s16le -ar 48000 pipe:1"
			})!;
			_ffmpeg.Start();
		}
		
		public async Task Play()
		{
			if (_nowPlaying is null)
				await GetNextAsync();
			
			if (_nowPlaying is null)
				throw new InvalidOperationException("Nothing to play.");

			var vnextSink = _connection!.GetTransmitSink();
			vnextSink.VolumeModifier = 0.6;
			
			StartFFMPeg();
			await _ffmpeg!.StandardOutput.BaseStream.CopyToAsync(vnextSink);
		}
		
		public void Add(SilkMusicResult music) => _queue.Enqueue(music);

		public void Remove(int count)
		{
			for (int i = 0; i < Math.Min(count, _queue.Count); i++)
				_queue.TryDequeue(out _);
		}

		
		//TODO: Playlist support, hence Task<T>
		public Task<SilkMusicResult?> GetNextAsync()
		{
			_queue.TryDequeue(out _nowPlaying);
			return Task.FromResult(_nowPlaying);
		}

	}
}