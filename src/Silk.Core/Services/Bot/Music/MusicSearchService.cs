using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Silk.Core.Utilities.HttpClient;
using Silk.Extensions;
using Silk.Extensions.DSharpPlus;
using YoutubeExplode;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;

namespace Silk.Core.Services.Bot.Music
{
	//TODO: Playlist support.
	public sealed class MusicSearchService
	{
		private readonly HttpClient _htClient;
		private readonly YoutubeClient _ytClient;
		private readonly DiscordShardedClient _dcClient;
		
		public MusicSearchService(YoutubeClient ytClient, DiscordShardedClient dcClient, IHttpClientFactory htClient)
		{
			_ytClient = ytClient;
			_dcClient = dcClient;
			_htClient = htClient.CreateSilkClient();
		}

		/// <summary>
		/// Searches <a href="https://youtube.com/"/> for videos, returning the first 10 results.
		/// </summary>
		/// <param name="query">The query to pass.</param>
		/// <param name="token">A cancellation token to cancel the </param>
		/// <returns></returns>
		public async Task<VideoSearchResult[]> SearchYouTubeAsync(string query)
		{
			var tcs = new CancellationTokenSource();
			var results = Array.Empty<VideoSearchResult>();
			var ytResult = _ytClient.Search.GetResultBatchesAsync(query, tcs.Token);
			try
			{
				await foreach (var res in ytResult.WithCancellation(tcs.Token))
				{
					tcs.Cancel(); // Will cancel enumeration. //
					results = res.Items.Take(10).Cast<VideoSearchResult>().ToArray();
				}
			}
			catch (TaskCanceledException) { } // I know //

			return results;
		}

		/// <summary>
		/// Gets a Video from <a href="https://youtube.com/"/>.
		/// </summary>
		/// <param name="url">The URL of the video to get.</param>
		/// <param name="requester">The user that requested the video.</param>
		/// <returns>The requested video, wrapped as <see cref="SilkMusicResult"/>.</returns>
		public async Task<SilkMusicResult> GetVideoFromLinkAsync(string url, DiscordUser requester)
		{
			var video = await _ytClient.Videos.GetAsync(VideoId.Parse(url));
			var videoManifest = await _ytClient.Videos.Streams.GetManifestAsync(video.Id);
			
			var audioStream = videoManifest.GetAudioOnlyStreams().LastOrDefault(b => b.AudioCodec == "opus")!;
			var content = (await _htClient.GetAsync(audioStream.Url, HttpCompletionOption.ResponseHeadersRead)).Content;

			var s = new MemoryStream();
			await content.CopyToAsync(s);
			
			return new() {Video = video, RequestedBy = requester, Duration = video.Duration.Value, AudioStream = s};
		}
		
		/// <summary>
		/// Waits for a user to make a selection based on the available selection.
		/// </summary>
		/// <param name="guild">The guild this search was performed on.</param>
		/// <param name="channel">The channel this search was performeed in.</param>
		/// <param name="user">The user that searched.</param>
		/// <param name="results">The search results from the user query to allow the user to select from.</param>
		/// <returns>A new <see cref="SilkMusicResult"/>, or null if the prompt timed out.</returns>
		public async Task<SilkMusicResult?> GetSelectionResultAsync(DiscordGuild guild, DiscordChannel channel, DiscordUser user, IReadOnlyList<VideoSearchResult> results)
		{
			if (!results.Any())
			{
				await channel.SendMessageAsync("Search yielded no results.");
				return null;
			}
			
			var interactivity = _dcClient.GetShard(guild).GetInteractivity();
			var token = new CancellationTokenSource(TimeSpan.FromSeconds(45));
			
			var pages = GeneratePagesFromSearch(results, user);
			// We don't want to block, we care about input! //
			_ = interactivity.SendPaginatedMessageAsync(channel, user, pages, token: token.Token);

			var message = await interactivity.WaitForMessageAsync(m => m.Author == user &&
			                                                           int.TryParse(m.Content, out int select) && select > 0 && select <= results.Count);

			if (!message.TimedOut)
			{
				var index = int.Parse(message.Result.Content);
				return await GetVideoFromLinkAsync(results[index - 1].Url, user);
			}
			else
			{
				return null;
			}
		}
		
		/// <summary>
		/// Generates two (2) <see cref="Page"/>s for use in Interactivity based on the provided set of <see cref="VideoSearchResult"/>.
		/// </summary>
		/// <param name="results">The result from querying the YouTube API.</param>
		/// <param name="user">The user who initiated the search.</param>
		/// <returns>Two (2) embeds wrapped in <see cref="Page"/>.</returns>
		private IEnumerable<Page> GeneratePagesFromSearch(IReadOnlyList<VideoSearchResult> results, DiscordUser user)
		{
			var pageOneResults = results.Take(5)
				.Select((r, i) => $"{i + 1}: **[{r.Title}]({r.Url})** by {r.Author.Title}\n\tDuration: `{(r.Duration.HasValue ? r.Duration.Value.ToString("h\\:mm\\:ss") : "LIVE")}`");
			var page1 = new DiscordEmbedBuilder()
				.WithAuthor(user.Username, user.GetUrl(), user.AvatarUrl)
				.WithColor(DiscordColor.Azure)
				.WithTitle("Search results:")
				.WithDescription(pageOneResults.Join("\n"));

			if (results.Count <= 5)
			{
				return new Page[] {new(embed: page1)};
			}
			else
			{
				// +6 to account for the fact that we're skipping 5 results.
				var pageTwoResults = results.Skip(5)
					.Select((r, i) => $"{i + 6}: **[{r.Title}]({r.Url})** by {r.Author.Title}\n\tDuration: `{r.Duration.Value:h\\:mm\\:ss}`");

				var page2 = new DiscordEmbedBuilder(page1.Build()).WithDescription(pageTwoResults.Join("\n"));

				return new Page[] {new(embed: page1), new(embed: page2)};
			}
		}
	}
}