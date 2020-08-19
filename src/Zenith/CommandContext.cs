using System;
using System.Collections.Generic;

namespace Zenith
{
	public class CommandContext
	{
		public string Sql { get; set; }
		public SqlTypeEnum Type { get; set; }
		public List<SingleParameter> SingleParameters { get; set; }
		public List<ObjectParameter> ObjectParameters { get; set; }
		public Dictionary<string, object> MiddlewareSwitches { get; set; }
		public IServiceProvider Container { get; internal set; }
	}
}