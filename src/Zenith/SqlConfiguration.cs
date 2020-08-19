using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zenith
{
	/// <summary>
	/// Profile level configuration for Zenith sql components
	/// </summary>
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

		/// <summary>
		/// Add a function that will be invoked on every database command that can view and modify command infomation before execution 
		/// or view and modify the return data before it is returned to the consumer
		/// </summary>
		/// 
		/// <example>
		/// <code>
		///	options.AddMiddleware((context, next) =>
		///	{
		///		if ((SqlTypeEnum.Update | SqlTypeEnum.Insert).HasFlag(context.Type))
		///		{
		///			foreach (var item in context.ObjectParameters)
		///			{
		///				if (item.Data is IUpdatable u)
		///				{
		///					u.UpdatedOn = DateTime.UtcNow;
		///				}
		///			}
		///		}
		///
		///		return next();
		///	});
		/// 
		/// </code>
		/// </example>
		/// <param name="middleware"></param>
		public void AddMiddleware(Func<CommandContext, Func<Task<IAsyncEnumerable<object>>>, Task<IAsyncEnumerable<object>>> middleware)
		{
			middlewares.Add(middleware);
		}

		internal List<Func<CommandContext, Func<Task<IAsyncEnumerable<object>>>, Task<IAsyncEnumerable<object>>>> middlewares =
								new List<Func<CommandContext, Func<Task<IAsyncEnumerable<object>>>, Task<IAsyncEnumerable<object>>>>();
	}
}