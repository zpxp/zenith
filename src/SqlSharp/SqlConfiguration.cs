using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SqlSharp
{
	public class SqlConfiguration
	{
		/// <summary>
		/// The name of this SqlConfiguration profile. Defaults to "default"
		/// </summary>
		public string Profile { get; set; } = "default";
		/// <summary>
		/// Database specific provider 
		/// </summary>
		public ISqlProvider Provider { get; set; }
		/// <summary>
		/// Connection string for this profile
		/// </summary>
		public string ConnectionString { get; set; }

		public void AddMiddleware(Func<CommandContext, Func<Task<IAsyncEnumerable<object>>>, Task<IAsyncEnumerable<object>>> middleware)
		{
			middlewares.Add(middleware);
		}

		internal List<Func<CommandContext, Func<Task<IAsyncEnumerable<object>>>, Task<IAsyncEnumerable<object>>>> middlewares = 
								new List<Func<CommandContext, Func<Task<IAsyncEnumerable<object>>>, Task<IAsyncEnumerable<object>>>>();
	}
}