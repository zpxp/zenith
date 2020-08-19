using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace Zenith.Extensions
{
	/// <summary>
	/// 
	/// </summary>
	public static class SqlExtensions
	{
		/// <summary>
		/// Get the keyname of a class type
		/// </summary>
		public static string GetPKColumn(this Type classType)
		{
			if (classType == null)
			{
				return null;
			}

			//flatten lists
			while (typeof(ICollection).IsAssignableFrom(classType) && !classType.IsArray)
			{
				classType = classType.GetGenericArguments().Single();
			}

			if (classType != null && SqlMappableAttribute.GetAttribute(classType, out var mappable))
			{
				var keyProp = classType.GetProperty(mappable.KeyName);
				if (keyProp == null)
				{
					return mappable.KeyName;
				}
				else if (SqlColumnAttribute.GetAttribute(keyProp, out var colName))
				{
					return colName.ColumnName;
				}
				else
				{
					return keyProp.Name;
				}
			}
			else
			{
				return null;
			}
		}


		/// <summary>
		/// Gets the name of the table colulm represented by this property
		/// </summary>
		/// <param name="prop"></param>
		/// <returns></returns>
		public static string GetSqlColumnName(this PropertyInfo prop)
		{
			if (SqlColumnAttribute.GetAttribute(prop, out var colName))
			{
				return colName.ColumnName;
			}
			else
			{
				return prop.Name;
			}
		}

		/// <summary>
		/// Strip off nullable wrapper type
		/// </summary>
		public static Type ReduceNullable(this Type type)
		{
			while (type.IsNullableType())
			{
				type = type.GetGenericArguments()[0];
			}
			return type;
		}

		/// <summary>
		/// Is type a 'simple' type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsSimple(this Type type)
		{
			if (type.IsNullableType())
			{
				// nullable type, check if the nested type is simple.
				return IsSimple(type.ReduceNullable());
			}
			return type.IsPrimitive
			  || type.IsEnum
			  || (type.IsArray && type.GetElementType().IsSimple())
			  || type.Equals(typeof(string))
			  || type.Equals(typeof(Guid))
			  || type.Equals(typeof(DateTime))
			  || type.Equals(typeof(decimal));
		}

		/// <summary>
		/// Is type a nullable struct type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsNullableType(this Type type)
		{
			return type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
		}

		/// <summary>
		/// Strip of generic collection types
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static Type ReduceList(this Type type)
		{
			while (typeof(ICollection).IsAssignableFrom(type) && !type.IsArray)
			{
				type = type.GetGenericArguments().Single();
			}
			return type;
		}

		/// <summary>
		/// Is type a generic collection type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsListType(this Type type)
		{
			return typeof(ICollection).IsAssignableFrom(type) && !type.IsArray;
		}

	}
}