using System;
using System.Reflection;

namespace SqlSharp
{

	[System.AttributeUsage(System.AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public sealed class SqlMappableAttribute : System.Attribute
	{

		public SqlMappableAttribute(string keyName)
		{
			KeyName = keyName;
		}

		public SqlMappableAttribute(string keyName, string tableName)
		{
			KeyName = keyName;
			TableName = tableName;
		}

		public string KeyName { get; }
		public string TableName { get; }

		public static bool GetAttribute(Type @class, out SqlMappableAttribute attribute)
		{
			if (IsDefined(@class, typeof(SqlMappableAttribute), true))
			{
				//does inherit
				attribute = @class.GetCustomAttribute<SqlMappableAttribute>(true);
				return attribute != null;
			}
			else
			{
				attribute = null;
				return false;
			}
		}


		public static SqlMappableAttribute GetAttribute(Type @class)
		{
			if (IsDefined(@class, typeof(SqlMappableAttribute), true))
			{
				//does not inherit
				return @class.GetCustomAttribute<SqlMappableAttribute>(true);
			}
			else
			{
				return null;
			}
		}
	}
}