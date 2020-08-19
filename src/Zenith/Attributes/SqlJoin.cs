using System;
using System.Reflection;

namespace Zenith
{

	[System.AttributeUsage(System.AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public sealed class SqlJoinAttribute : System.Attribute
	{
		public SqlJoinAttribute(string alias, params Type[] intermediaryJoins)
		{
			Alias = alias;
			IntermediaryJoins = intermediaryJoins;
		}

		public string Alias { get; }
		/// <summary>
		/// Condition appended to the end of the join when generating select statements
		/// </summary>
		public string Condition { get; set; }
		/// <summary>
		/// The type of join to use when generating select statements. Defaults to LEFT join
		/// </summary>
		public JoinEnum JoinType { get; set; }
		public Type[] IntermediaryJoins { get; }

		public static bool GetAttribute(PropertyInfo prop, out SqlJoinAttribute attribute)
		{
			if (IsDefined(prop, typeof(SqlJoinAttribute), false))
			{
				//does not inherit
				attribute = prop.GetCustomAttribute<SqlJoinAttribute>(false);
				return attribute != null;
			}
			else
			{
				attribute = null;
				return false;
			}
		}


		public static SqlJoinAttribute GetAttribute(PropertyInfo prop)
		{
			if (IsDefined(prop, typeof(SqlJoinAttribute), false))
			{
				//does not inherit
				return prop.GetCustomAttribute<SqlJoinAttribute>(false);
			}
			else
			{
				return null;
			}
		}

		public enum JoinEnum
		{
			Left = 0,
			Full = 1,
			Right = 3
		}
	}
}