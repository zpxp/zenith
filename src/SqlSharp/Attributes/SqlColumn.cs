using System.Reflection;

namespace SqlSharp
{

	[System.AttributeUsage(System.AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
	public sealed class SqlColumnAttribute : System.Attribute
	{
		public SqlColumnAttribute(string columnName)
		{
			ColumnName = columnName;
		}

		public string ColumnName { get; }

		public static bool GetAttribute(PropertyInfo prop, out SqlColumnAttribute attribute)
		{
			if (IsDefined(prop, typeof(SqlColumnAttribute), true))
			{
				//does inherit
				attribute = prop.GetCustomAttribute<SqlColumnAttribute>(true);
				return attribute != null;
			}
			else
			{
				attribute = null;
				return false;
			}
		}


		public static SqlColumnAttribute GetAttribute(PropertyInfo prop)
		{
			if (IsDefined(prop, typeof(SqlColumnAttribute), true))
			{
				//does not inherit
				return prop.GetCustomAttribute<SqlColumnAttribute>(true);
			}
			else
			{
				return null;
			}
		}
	}
}