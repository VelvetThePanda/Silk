using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Serilog;
using Silk.Extensions;

namespace Silk.Core.Services.Bot.Music
{
	public sealed class SilkMusicQueue : IDisposable
	{
		/// <summary>
		/// Gets or sets whether or not the current queue is paused. 
		/// </summary>
		public bool IsPaused { get; set; }

		public CancellationTokenSource TokenSource { get; private set; } = new();
		public CancellationToken CancellationToken => TokenSource.Token;
		
		/// <summary>
		/// .NET -> FFMpeg
		/// </summary>
		public Stream StreamInput { get; }
		
		/// <summary>
		/// FFMpeg -> VNext
		/// </summary>
		public Stream StreamOutput { get; }
		
		/// <summary>
		/// The currently playing song, if any. Otherwise null.
		/// </summary>
		public SilkMusicResult? NowPlaying => _nowPlaying;
		private SilkMusicResult? _nowPlaying;
		
		/// <summary>
		/// The queue of songs to be played.
		/// </summary>
		public ConcurrentQueue<SilkMusicResult> Queue { get; } = new();
		
		/// <summary>
		/// The remaining time of the currently playing song.
		/// </summary>
		public TimeSpan RemainingDuration => _nowPlaying is null ? TimeSpan.Zero : 
			TimeSpan.FromMilliseconds(_nowPlaying.Duration.TotalMilliseconds - _nowPlaying.AudioStream.Length /*bytes*/ / _nowPlaying.Duration.TotalMilliseconds * _nowPlaying.AudioStream.Position);
		
		/// <summary>
		/// Queues the next song.
		/// </summary>
		public void Next() => Queue.TryDequeue(out _nowPlaying);

		public void Resume()
		{
			if (IsPaused)
			{
				IsPaused = false;
				TokenSource.TryCancel();
				TokenSource = new();
			}
		}
		public void Pause()
		{
			if (!IsPaused)
			{
				IsPaused = true;
				TokenSource.Cancel();
			}
		}

		private readonly Process _ffmpeg;
		public SilkMusicQueue()
		{
			_ffmpeg = Process.Start(new ProcessStartInfo()
			{
				Arguments = "-i - -ac 2 -f s16le -ar 48000 pipe:1 -fflags nobuffer",
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				FileName = "./ffmpeg",
				CreateNoWindow = true,
				UseShellExecute = false,
			})!;

			StreamInput = _ffmpeg.StandardInput.BaseStream;
			StreamOutput = _ffmpeg.StandardOutput.BaseStream;
			_ffmpeg.Start();
			_ffmpeg.Exited += (sender, args) => Log.Fatal("FFMpeg has exited with code {Code}", _ffmpeg.ExitCode);
			_ffmpeg.Disposed += (sender, args) => Log.Fatal("FFMpeg was disposed.");
			_ffmpeg.ErrorDataReceived += (_, e) => Log.Fatal("FFMpeg errored! {Error}", e.Data);
		}

		~SilkMusicQueue() => Dispose();
		
		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_ffmpeg.Dispose();
			StreamInput.Dispose();
			StreamOutput.Dispose();
		}
	}
}