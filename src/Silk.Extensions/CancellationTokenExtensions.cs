using System;
using System.Threading;

namespace Silk.Extensions
{
	public static class CancellationTokenExtensions
	{
		public static bool TryCancel(this CancellationTokenSource? cts)
		{
			try
			{
				cts?.Cancel();
				return true;
			}
			catch (ObjectDisposedException)
			{
				return false;
			}
		}
	}
}