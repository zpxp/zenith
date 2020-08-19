using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Zenith
{

	[System.AttributeUsage(System.AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	public sealed class SqlForeignKeyAttribute : System.Attribute
	{

		public SqlForeignKeyAttribute(Type joinTable)
		{
			JoinTable = joinTable;
		}


		public SqlForeignKeyAttribute(string joinAlias, Type joinTable)
		{
			JoinAlias = joinAlias;
			JoinTable = joinTable;
		}

		public string JoinAlias { get; }
		public Type JoinTable { get; }

		public static bool GetAttributes(PropertyInfo prop, out List<SqlForeignKeyAttribute> attributes)
		{
			if (IsDefined(prop, typeof(SqlForeignKeyAttribute), false))
			{
				//does not inherit
				attributes = prop.GetCustomAttributes<SqlForeignKeyAttribute>(false).ToList();
				return attributes.Count > 0;
			}
			else
			{
				attributes = new List<SqlForeignKeyAttribute>();
				return false;
			}
		}


		public static List<SqlForeignKeyAttribute> GetAttributes(PropertyInfo prop)
		{
			if (IsDefined(prop, typeof(SqlForeignKeyAttribute), false))
			{
				//does not inherit
				return prop.GetCustomAttributes<SqlForeignKeyAttribute>(false).ToList();
			}
			else
			{
				return new List<SqlForeignKeyAttribute>();
			}
		}
	}
}