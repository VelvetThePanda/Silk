using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Silk.Core.Utilities.HttpClient
{
	// https://stackoverflow.com/a/19650002 //
	public sealed class RetryHandler : DelegatingHandler
	{
		// Strongly consider limiting the number of retries - "retry forever" is
		// probably not the most user friendly way you could respond to "the
		// network cable got pulled out."
		private const int MaxRetries = 3;
		
		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			HttpResponseMessage? response = null;
			for (int i = 0; i < MaxRetries; i++)
			{
				response = await base.SendAsync(request, cancellationToken);
				if (response.IsSuccessStatusCode)
					return response;
				Log.Verbose("Retry {Retry} of {Max}", i + 1, MaxRetries);
			}
			
			return response!;
		}
	}
}