using System;
using System.Reflection;

namespace SqlSharp
{
	/// <summary>
	/// Property Ignore flags used with `[SqlIgnoreAttribute]`
	/// </summary>
	[Flags]
	public enum SqlIgnoreFlags
	{
		/// <summary>
		/// Do not ignore
		/// </summary>
		None = 0,

		/// <summary>
		/// Ignore this property in `AddArguments` calls
		/// </summary>
		AddArguments = 1,

		/// <summary>
		/// Ignore this property in all create select statements
		/// </summary>
		CreateSelect = 1 << 1,

		/// <summary>
		/// Ignore this property in all create insert statements
		/// </summary>
		CreateInsert = 1 << 2,

		/// <summary>
		/// Ignore this property in mapping data to objects from raw data reader
		/// </summary>
		Read = 1 << 3,

		/// <summary>
		/// Always ignore this property
		/// </summary>
		All = int.MaxValue
	}



	/// <summary>
	/// Decorate table properties to ignore them under certian scenarios
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	public sealed class SqlIgnoreAttribute : System.Attribute
	{
		public SqlIgnoreAttribute(SqlIgnoreFlags flags = SqlIgnoreFlags.All)
		{
			Flags = flags;
		}

		public SqlIgnoreFlags Flags { get; }


		public static bool GetAttribute(PropertyInfo prop, out SqlIgnoreAttribute attribute)
		{
			if (IsDefined(prop, typeof(SqlIgnoreAttribute), false))
			{
				//does not inherit
				attribute = prop.GetCustomAttribute<SqlIgnoreAttribute>(false);
				return attribute != null;
			}
			else
			{
				attribute = null;
				return false;
			}
		}

		/// <summary>
		/// Return true if the given property is ignored
		/// </summary>
		/// <param name="prop"></param>
		/// <param name="flag">Flag to check</param>
		/// <returns></returns>
		public static bool IsIgnored(PropertyInfo prop, SqlIgnoreFlags flag)
		{
			if (GetAttribute(prop, out var attr))
			{
				return attr.Flags.HasFlag(flag);
			}
			return false;
		}
	}

}