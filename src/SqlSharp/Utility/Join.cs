using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SqlSharp.Exceptions;
using SqlSharp.Extensions;

namespace SqlSharp.Utility
{
	public class Join
	{
		public Join(Type leftType, Type rightType, string leftPk, string rightPk, string fk, PropertyInfo leftToRightProp, FkExistsIn side)
		{
			LeftType = leftType;
			RightType = rightType;
			LeftPk = leftPk;
			RightPk = rightPk;
			Fk = fk;
			LeftToRightProp = leftToRightProp;
			Side = side;
		}

		public Type LeftType { get; }
		public Type RightType { get; }
		public string LeftPk { get; }
		public string RightPk { get; }
		public string Fk { get; }
		public PropertyInfo LeftToRightProp { get; }
		public FkExistsIn Side { get; }

		public static Join Create(Type leftType, Type rightType, string joinAlias)
		{
			var leftPk = leftType.GetPKColumn();
			var rightPk = rightType.GetPKColumn();

			if (string.IsNullOrWhiteSpace(leftPk) && string.IsNullOrWhiteSpace(rightPk))
			{
				throw new SqlJoinException($"Cannot join types {leftType.Name} and {rightType.Name}. One or both do not specify [SqlColumnAttribute] attributes");
			}
			// see which way the join runs
			var leftToRightProp = GetRelationship(leftType, rightType, leftPk, joinAlias);

			if (leftToRightProp != null)
			{
				string fk = leftToRightProp.Name;
				var dbCol = SqlColumnAttribute.GetAttribute(leftToRightProp);
				if (dbCol != null)
				{
					fk = dbCol.ColumnName;
				}
				return new Join(leftType, rightType, leftPk, rightPk, fk, leftToRightProp, FkExistsIn.RightSide);
			}
			else
			{
				// try flip types
				var rightToLeftProp = GetRelationship(rightType, leftType, rightPk, joinAlias);
				if (rightToLeftProp == null)
				{
					throw new SqlJoinException($"Cannot join types {leftType.Name} and {rightType.Name}. Neither contains the other types Primary Key. Cannot establish a relationship");
				}
				else
				{
					string fk = rightToLeftProp.Name;
					var dbCol = SqlColumnAttribute.GetAttribute(rightToLeftProp);
					if (dbCol != null)
					{
						fk = dbCol.ColumnName;
					}
					return new Join(leftType, rightType, leftPk, rightPk, fk, rightToLeftProp, FkExistsIn.LeftSide);
				}
			}
		}


		/// <summary>
		/// Get the prop from type2 that is the foreign key to type1, if exists
		/// </summary>
		/// <param name="leftType"></param>
		/// <param name="rightType"></param>
		/// <param name="type1Pk"></param>
		/// <param name="joinAlias">the alias from the SqlJoin attribute</param>
		/// <returns></returns>
		static PropertyInfo GetRelationship(Type leftType, Type rightType, string type1Pk, string joinAlias)
		{
			var foreignKeyProps = rightType.GetProperties()
											.Select(x => new FKProp
											{
												prop = x,
												FKs = SqlForeignKeyAttribute.GetAttributes(x)
													.Where(fk => fk != null && (fk.JoinTable == leftType || leftType.IsSubclassOf(fk.JoinTable)))
													.ToList()
											})
											.Where(prop => prop.FKs != null && prop.FKs.Count > 0)
											.ToList();

			if (foreignKeyProps.Count > 1)
			{
				// first check alias
				var col = foreignKeyProps.FirstOrDefault(p => p.FKs.Any(x => x.JoinAlias == joinAlias));

				if (col != null)
				{
					// found one with alias. use this
					return col.prop;
				}

				// now try match on prop name
				// get the one whos name matches type1Pk
				col = foreignKeyProps.FirstOrDefault(x => x.prop.Name == type1Pk);
				if (col == null)
				{
					string fks = string.Join("\n\t", foreignKeyProps.Select(x => x.prop.Name));
					throw new SqlJoinException($"Cannot extablish relationship between {leftType.Name} and {rightType.Name}. {rightType.Name} contains foreign key relations to {leftType.Name}, but none match the name {type1Pk}.\nForeign keys: {fks}");
				}
				else
				{
					return col.prop;
				}
			}
			else if (foreignKeyProps.Count == 1)
			{
				// one fk was found. return it
				return foreignKeyProps[0].prop;
			}
			else
			{
				// type defined no [SqlForeignKey] attrs. Try and find matching col names instead
				return rightType.GetProperty(type1Pk);
			}
		}

		class FKProp
		{
			public PropertyInfo prop;
			public List<SqlForeignKeyAttribute> FKs;
		}

		public enum FkExistsIn
		{
			LeftSide,
			RightSide
		}
	}
}