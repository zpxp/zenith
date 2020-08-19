using System;
using System.Threading;

namespace Zenith.Utility
{

	/// <summary>
	/// Set Token to a cancelled state when token is cancelled or this object is disposed
	/// </summary>
	internal class TokenLink : IDisposable
	{
		private readonly CancellationToken token;
		private readonly CancellationTokenSource canceller;
		private readonly CancellationTokenSource link;

		public CancellationToken Token => link.Token;

		public TokenLink(CancellationToken token)
		{
			this.token = token;
			this.canceller = new CancellationTokenSource();
			this.link = CancellationTokenSource.CreateLinkedTokenSource(token, this.canceller.Token);
		}

		public void Dispose()
		{
			canceller.Cancel();
		}
	}
}