using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Silk.Extensions;
using Silk.Extensions.DSharpPlus;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace Silk.Core.Services.Bot.Music
{
	public sealed class MusicSearchService
	{
		private readonly YoutubeClient _ytClient;
		private readonly DiscordShardedClient _dcClient;
		public MusicSearchService(YoutubeClient ytClient, DiscordShardedClient dcClient)
		{
			_ytClient = ytClient;
			_dcClient = dcClient;
		}
		
		/// <summary>
		/// Searches <a href="https://youtube.com/"/> for videos, returning the first 10 results.
		/// </summary>
		/// <param name="query">The query to pass.</param>
		/// <param name="token">A cancellation token to cancel the </param>
		/// <returns></returns>
		public async IAsyncEnumerable<VideoSearchResult> SearchYouTubeAsync(string query, CancellationToken token = default)
		{
			var results = await _ytClient.Search.GetVideosAsync(query, token);
			
			for (int i = 0; i < Math.Min(results.Count, 10); i++)
				yield return results[i];
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
		
			var videoStream = videoManifest.GetAudioStreams().GetWithHighestBitrate();

			var stream = await _ytClient.Videos.Streams.GetAsync(videoStream);
			
			return new() {RequestedBy = requester, Duration = video.Duration.Value, AudioStream = stream};
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
			var interactivity = _dcClient.GetShard(guild).GetInteractivity();
			var token = new CancellationTokenSource(TimeSpan.FromSeconds(45));
			
			//I'm sorry for the pauses; people keep messaging me. //
			var pages = GeneratePagesFromSearch(results, user);
			await interactivity.SendPaginatedMessageAsync(channel, user, pages, token: token.Token);

			var message = await interactivity.WaitForMessageAsync(m => m.Author == user &&
			                                                           int.TryParse(m.Content, out int select) && select < 1 && select >= results.Count);

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
				.Select((r, i) => $"{i + 1}: {r.Title} by {r.Author.Title}\n\tDuration: `{r.Duration.Value:H:mm:ss}`");
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
					.Select((r, i) => $"{i + 6}: {r.Title} by {r.Author.Title}\n\tDuration: `{r.Duration.Value:H:mm:ss}`");

				var page2 = new DiscordEmbedBuilder(page1.Build()).WithDescription(pageTwoResults.Join("\n"));

				return new Page[] {new(embed: page1), new(embed: page2)};
			}
		}
	}
}