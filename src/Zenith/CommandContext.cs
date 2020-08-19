using System;
using System.Collections.Generic;

namespace Zenith
{

	/// <summary>
	/// Contextual infomation describing the current command used in sql middleware. See `SqlConfiguration.AddMiddleware`
	/// </summary>
	public class CommandContext
	{
		/// <summary>
		/// The command to be run
		/// </summary>
		/// <value></value>
		public string Sql { get; set; }
		/// <summary>
		/// Type of command to be run
		/// </summary>
		/// <value></value>
		public SqlTypeEnum Type { get; set; }
		/// <summary>
		/// Parameters added with `ISqlCommand.AddArgument`
		/// </summary>
		/// <value></value>
		public List<SingleParameter> SingleParameters { get; set; }
		/// <summary>
		/// Parameters added with `ISqlCommand.AddArguments`
		/// </summary>
		/// <value></value>
		public List<ObjectParameter> ObjectParameters { get; set; }
		/// <summary>
		/// Switches added with `ISqlCommand.AddSwitch`
		/// </summary>
		/// <value></value>
		public Dictionary<string, object> MiddlewareSwitches { get; set; }
		/// <summary>
		/// Service container with the same lifetime as the executing `IUnitOfWork`
		/// </summary>
		/// <value></value>
		public IServiceProvider Container { get; internal set; }
	}
}