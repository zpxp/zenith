using System;
using System.Reflection;

namespace SqlSharp
{
	public class SingleParameter
	{
		public string ParameterName { get; set; }
		public Type DataType { get; set; }
		public object Data { get; set; }
	}

	public class ObjectParameter
	{
		public Type DataType { get; set; }
		public object Data { get; set; }
		public Func<PropertyInfo, object, bool> Predicate { get; set; }
	}
}