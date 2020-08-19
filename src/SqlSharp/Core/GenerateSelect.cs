using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using SqlSharp.Exceptions;
using SqlSharp.Extensions;
using SqlSharp.Utility;

namespace SqlSharp.Core
{
	/// <summary>
	/// Class used to generate select statements. This class is thread safe
	/// </summary>
	public class GenerateSelect
	{
		private static ConcurrentDictionary<string, Dictionary<string, GSTable>> cache = new ConcurrentDictionary<string, Dictionary<string, GSTable>>();
		private readonly Config config;
		public GenerateSelect(Config config = null)
		{
			this.config = config ?? new Config();
		}


		private void GenerateTable(GenerateSelectOptions options, Type tableType, string alias, GSTable parentTable, Dictionary<string, GSTable> tables, SqlJoinAttribute joinAttr)
		{
			var table = TryAddTable(alias, parentTable, tableType, joinAttr, tables);
			IterateTypeProps(options, tableType, table, parentTable, tables);
		}

		private void IterateTypeProps(GenerateSelectOptions options, Type tableType, GSTable table, GSTable parentTable, Dictionary<string, GSTable> tables)
		{
			foreach (var prop in tableType.GetProperties().Where(x => !SqlIgnoreAttribute.IsIgnored(x, SqlIgnoreFlags.CreateSelect)))
			{
				var propType = prop.PropertyType.ReduceNullable().ReduceList();
				if (SqlJoinAttribute.GetAttribute(prop, out var joinAttr))
				{
					List<Type> joins = joinAttr.IntermediaryJoins.ToList();
					if (!propType.IsSimple() && !propType.IsInterface)
					{
						// add proptype as the last table to be joined to we can complete join chain
						joins.Add(propType);
					}

					if (joins.Count == 0)
					{
						throw new GenerateSelectException($"Cannot use [SqlJoinAttribute] on property '{prop.Name}' without specifying at least one type in the SqlJoinAttribute IntermediaryJoins argument");
					}


					if (propType.IsInterface)
					{
						var implentationType = joins.Last();
						//see if concrete implementation actually has the interface
						if (!propType.IsAssignableFrom(implentationType))
						{
							throw new GenerateSelectException($"Cannot apply join on property '{propType.Name} {prop.Name}'. Type '{implentationType}' does not implement the interface '{propType.Name}'");
						}
					}

					Type lastType = tableType;
					GSTable localTable = table;
					GSTable lastTable = table;
					string joinAlias = table.isRoot ? joinAttr.Alias : table.tableAlias + "_" + joinAttr.Alias;
					for (int i = 0; i < joins.Count; lastType = joins[i++], lastTable = localTable)
					{
						var joinType = joins[i];
						Join joinDef;
						try
						{
							joinDef = Join.Create(lastType, joinType, joinAttr.Alias);
						}
						catch (Exception e)
						{
							throw new GenerateSelectException($"Failed join on property '{prop.Name}' in type '{tableType.Name}'. See inner exception.", e);
						}

						if (i >= 1)
						{

							// dont add type namespace as well if its only a single join
							joinAlias += "_" + (options.JoinNameStrategy == TableJoinNameStrategyEnum.FirstLetter ? joinType.Name[0].ToString() : joinType.Name);
						}

						TryAddColumn(localTable, joinDef.LeftPk);
						localTable = TryAddTable(joinAlias, lastTable, joinType, joinAttr, tables);
						TryAddColumn(localTable, joinDef.RightPk);
						if (joinDef.Side == Join.FkExistsIn.LeftSide)
						{
							localTable.leftJoinKey = joinDef.RightPk;
							localTable.rightJoinKey = joinDef.Fk;
							TryAddColumn(lastTable, joinDef.Fk);
						}
						else
						{
							localTable.leftJoinKey = joinDef.Fk;
							localTable.rightJoinKey = joinDef.LeftPk;
							TryAddColumn(localTable, joinDef.Fk);
						}
					} //end for

					if (propType.IsSimple())
					{
						// only map the required column
						string name = prop.GetSqlColumnName();
						TryAddColumn(lastTable, name);
					}
					else
					{
						// is sub class. iterate type
						GenerateTable(options, lastType, joinAlias, lastTable, tables, joinAttr);
					}

					//assign condition to last join in chain
					lastTable.joinCondition = joinAttr.Condition;
				}
				else if (propType.IsSimple())
				{
					string name = prop.GetSqlColumnName();
					TryAddColumn(table, name);
				}
				else
				{
					// is sub class. iterate type
					IterateTypeProps(options, propType, table, parentTable, tables);
				}
			}
		}

		void TryAddColumn(GSTable table, string column)
		{
			if (this.config.IdentifierNameComparer == null ? !table.columns.Contains(column) : !table.columns.Contains(column, this.config.IdentifierNameComparer))
			{
				table.columns.Add(column);
			}
		}

		GSTable TryAddTable(string alias, GSTable parentAlias, Type tableType, SqlJoinAttribute joinAttr, Dictionary<string, GSTable> tables)
		{
			if (tables.ContainsKey(alias))
			{
				return tables[alias];
			}
			else
			{
				var table = new GSTable(tables.Count == 0, tableType, alias, parentAlias, joinAttr);
				tables.Add(alias, table);
				return table;
			}
		}

		private string BuildColumnList(GenerateSelectOptions options, Dictionary<string, GSTable> tables)
		{
			string sql = "";
			string colprefix = string.IsNullOrWhiteSpace(options.AliasPrefix) ? "" : options.AliasPrefix + "_";
			bool hasCol = false;

			foreach (var pair in tables)
			{
				var table = pair.Value;
				sql += $"\n -- Table `{colprefix + table.tableAlias}`\n";

				foreach (var col in table.columns)
				{
					if (table.isRoot)
					{
						sql += hasCol ? "\t," : "\t ";
						sql += $"{config.QuotePrefix}{table.tableAlias}{config.QuoteSuffix}.{config.QuotePrefix}{col}{config.QuoteSuffix} AS {config.QuotePrefix}{colprefix + col}{config.QuoteSuffix}\n";
					}
					else
					{
						sql += hasCol ? "\t," : "\t ";
						sql += $"{config.QuotePrefix}{table.tableAlias}{config.QuoteSuffix}.{config.QuotePrefix}{col}{config.QuoteSuffix} AS {config.QuotePrefix}{colprefix + table.tableAlias}_{col}{config.QuoteSuffix}\n";
					}
					hasCol = true;
				}

				sql += $" -- End Table `{colprefix + table.tableAlias}`\n";
			}

			return sql;
		}

		public string Generate(Type tableType, string rootAlias, GenerateSelectOptions options)
		{
			options ??= new GenerateSelectOptions();
			options.JoinNameStrategy ??= config.JoinNameStrategy;

			// <alias, table>
			Dictionary<string, GSTable> tables = new Dictionary<string, GSTable>(this.config.IdentifierNameComparer);

			if (config.CacheTypeRelationships)
			{
				string key = rootAlias + "~" + tableType.FullName;

				// get relationships from cache
				if (cache.ContainsKey(key))
				{
					tables = cache[key];
				}
				else
				{
					// construct all the relationships
					GenerateTable(options, tableType, rootAlias, null, tables, null);
					if (!cache.ContainsKey(key))
					{
						cache.TryAdd(key, tables);
					}
				}
			}
			else
			{
				// construct all the relationships
				GenerateTable(options, tableType, rootAlias, null, tables, null);
			}


			bool hasSchema = !string.IsNullOrWhiteSpace(options.Schema);
			var mappable = SqlMappableAttribute.GetAttribute(tableType);
			string sql = options.CreateSelectKeyword ? "SELECT\n" : "";

			if (options.CreateColumnList)
			{
				sql += BuildColumnList(options, tables);
			}

			if (options.CreateFrom)
			{
				if (string.IsNullOrWhiteSpace(mappable.TableName))
				{
					throw new GenerateSelectException($"[SqlMappableAttribute] on type '{tableType.Name}' does not specify TableName. Cannot create FROM.");
				}
				sql += $"\nFROM {options.Schema}{(hasSchema ? config.SchemaSeparator : null)}{config.QuotePrefix}{mappable.TableName}{config.QuoteSuffix} {config.QuotePrefix}{rootAlias}{config.QuoteSuffix}\n";
			}

			if (options.CreateJoins)
			{
				var tablesToJoin = tables.Where(x => !x.Value.isRoot).ToList();
				for (int i = 0; i < tablesToJoin.Count; i++)
				{
					var join = tablesToJoin[i].Value;
					var joinAlias = tablesToJoin[i].Key;

					string rightKey = null;
					string leftKey = null;

					var joinMappable = SqlMappableAttribute.GetAttribute(join.tableType);

					if (joinMappable == null)
					{
						throw new GenerateSelectException($"Type {join.tableType.Name} does not define [SqlMappableAttribute]. Cannot create select statement");
					}
					if (string.IsNullOrWhiteSpace(joinMappable.TableName))
					{
						throw new GenerateSelectException($"[SqlMappableAttribute] on type '{tableType.Name}' does not specify TableName. Cannot create JOIN.");
					}

					string tablename = joinMappable.TableName;

					rightKey = join.rightJoinKey;
					leftKey = join.leftJoinKey;

					//default to the right key
					leftKey = leftKey ?? rightKey;
					string type = join.joinAttr.JoinType switch
					{
						SqlJoinAttribute.JoinEnum.Left => "LEFT",
						SqlJoinAttribute.JoinEnum.Full => "",
						SqlJoinAttribute.JoinEnum.Right => "RIGHT",
						_ => throw new Exception("JoinType not found")
					};

					sql += type + $" JOIN {options.Schema}{(hasSchema ? config.SchemaSeparator : null)}{config.QuotePrefix}{tablename}{config.QuoteSuffix} {config.QuotePrefix}{joinAlias}{config.QuoteSuffix}" +
										$" ON {config.QuotePrefix}{join.parentTable.tableAlias}{config.QuoteSuffix}.{config.QuotePrefix}{rightKey}{config.QuoteSuffix}" +
										$" = {config.QuotePrefix}{joinAlias}{config.QuoteSuffix}.{config.QuotePrefix}{leftKey}{config.QuoteSuffix}";

					if (config.ExtendJoinCondition?.Invoke(join) is var result && !string.IsNullOrWhiteSpace(result))
					{
						sql += result;
					}

					if (!string.IsNullOrEmpty(join.joinCondition))
					{
						List<GSTable> contextJoins = new List<GSTable>();

						// walk up the join path and get all tables that a joined under this join
						for (var table = join; table.parentTable != null; table = table.parentTable)
						{
							contextJoins.Add(table);
						}

						// string.Format args for all joins starting at the deepest join
						string[] aliases = contextJoins.Select(x => x.tableAlias).Append(rootAlias).ToArray();

						sql += $" AND ({string.Format(join.joinCondition, aliases)})";
					}

					sql += "\n";
				}
			}

			if (options.Limit.HasValue)
			{
				sql += $"LIMIT ({options.Limit.Value})\n";
			}

			return sql;
		}


		/// <summary>
		/// Provider level config
		/// </summary>
		public class Config
		{
			/// <summary>
			/// Gets or sets the beginning character or characters to use when specifying database
			/// objects (for example, tables or columns)
			/// </summary>
			public virtual string QuotePrefix { get; set; } = "";

			/// <summary>
			/// Gets or sets the ending character or characters to use when specifying database
			/// objects (for example, tables or columns)
			/// </summary>
			public virtual string QuoteSuffix { get; set; } = "";

			/// <summary>
			/// Gets or sets the character to be used for the separator between the schema identifier
			/// and any other identifiers.
			/// </summary>
			public virtual string SchemaSeparator { get; set; } = ".";

			/// <summary>
			/// The IEqualityComparer used to compare identifier names
			/// </summary>
			public virtual IEqualityComparer<string> IdentifierNameComparer { get; set; }

			/// <summary>
			/// Cache the generated table relationships
			/// </summary>
			/// <value></value>
			public virtual bool CacheTypeRelationships { get; set; } = true;


			/// <summary>
			/// Extend join condition sql by returning additional conditions.
			/// </summary>
			/// <value></value>
			public virtual Func<GSTable, string> ExtendJoinCondition { get; set; }

			/// <summary>
			/// Naming strategy used in naming sub join table aliases
			/// </summary>
			public virtual TableJoinNameStrategyEnum JoinNameStrategy { get; set; } = TableJoinNameStrategyEnum.FirstLetter;
		}


	}

	/// <summary>
	/// Call level options
	/// </summary>
	public class GenerateSelectOptions
	{
		/// <summary>
		/// Naming strategy used in naming sub join table aliases
		/// </summary>
		public virtual TableJoinNameStrategyEnum? JoinNameStrategy { get; set; }

		/// <summary>
		/// Add the 'SELECT' keyword to the start of the generated script
		/// </summary>
		public virtual bool CreateSelectKeyword { get; set; } = true;

		/// <summary>
		/// Add 'FROM table' after the column list in the generated script
		/// </summary>
		public virtual bool CreateFrom { get; set; } = true;

		/// <summary>
		/// Add the list of columns in the generated script
		/// </summary>
		public virtual bool CreateColumnList { get; set; } = true;

		/// <summary>
		/// Add the joins after the 'FROM table' in the generated script
		/// </summary>
		public virtual bool CreateJoins { get; set; } = true;

		/// <summary>
		/// Table schema
		/// </summary>
		public virtual string Schema { get; set; } = "";

		/// <summary>
		/// Prefix for all aliases
		/// </summary>
		public virtual string AliasPrefix { get; set; }

		/// <summary>
		/// Add 'LIMIT X' to generated script
		/// </summary>
		public virtual int? Limit { get; set; }
	}

	public enum TableJoinNameStrategyEnum
	{
		FirstLetter = 0,
		FullName = 1
	}


	/// <summary>
	/// Represents a single table or table join in GenerateSelect logic
	/// </summary>
	public class GSTable
	{
		public readonly bool isRoot;
		public readonly string tableAlias;
		public readonly List<string> columns;
		public string leftJoinKey;
		public string rightJoinKey;
		public string joinCondition;
		public readonly SqlJoinAttribute joinAttr;
		public readonly Type tableType;
		public readonly GSTable parentTable;

		public GSTable(bool isRoot, Type tableType, string tableAlias, GSTable parentTable, SqlJoinAttribute joinAttr, List<string> columns = null)
		{
			this.parentTable = parentTable;
			this.tableType = tableType;
			this.joinAttr = joinAttr;
			this.isRoot = isRoot;
			this.tableAlias = tableAlias;
			this.columns = columns ?? new List<string>();
		}
	}


}