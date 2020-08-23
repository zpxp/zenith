using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Zenith.Exceptions;
using Zenith.Extensions;
using Zenith.Utility;

namespace Zenith.Core
{
	/// <summary>
	/// Class used to map data
	/// </summary>
	public class DataMapper
	{

		private readonly Config config;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="config"></param>
		public DataMapper(Config config = null)
		{
			this.config = config ?? new Config();
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dataType"></param>
		/// <param name="reader"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public IAsyncEnumerable<object> MapStream(Type dataType, DbDataReader reader, CancellationToken token)
		{
			var mapper = new MapInstance(dataType, reader, config, token);
			return mapper.MapAsync();
		}


		/// <summary>
		/// Instance based configuration for DataMapper
		/// </summary>
		public class Config
		{
			/// <summary>
			/// The IEqualityComparer used to compare identifier names
			/// </summary>
			public virtual IEqualityComparer<string> IdentifierNameComparer { get; set; }

			/// <summary>
			/// Naming strategy used in naming sub join table aliases
			/// </summary>
			public virtual TableJoinNameStrategyEnum JoinNameStrategy { get; set; } = TableJoinNameStrategyEnum.FullName;
		}
	}


	class MapInstance
	{
		private readonly DbDataReader reader;
		private readonly CancellationToken token;
		private readonly Type dataType;
		private readonly string pkColumn;
		private readonly DataMapper.Config config;
		private Dictionary<object, DataMapperNode> rootNodes = new Dictionary<object, DataMapperNode>();

		public MapInstance(Type dataType, DbDataReader reader, DataMapper.Config config, CancellationToken token)
		{
			this.config = config;
			this.token = token;
			this.reader = reader;
			this.dataType = dataType.ReduceList();
			this.pkColumn = dataType.GetPKColumn();
			if (pkColumn == null)
			{
				throw new SqlMapException($"Cannot determine mapping key on type '{dataType.Name}'. Did you forget to add [SqlMappableAttribute]?");
			}
		}


		private async Task ReadData()
		{
			while (await reader.ReadAsync(token))
			{
				var row = Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, reader.GetValue, config.IdentifierNameComparer);
				var pk = row[pkColumn];
				if (!rootNodes.ContainsKey(pk))
				{
					rootNodes.Add(pk, new DataMapperNode(dataType, this, pkColumn));
				}
				rootNodes[pk].AddData(row);
			}
		}

		internal async IAsyncEnumerable<object> MapAsync()
		{
			//begin the db read
			await ReadData();

			foreach (var node in rootNodes)
			{
				yield return node.Value.Map();
			}
		}

		class DataMapperNode
		{
			internal DataMapperNode(Type dataType, MapInstance mapper, string keyName)
			{
				this.dataType = dataType;
				this.mapper = mapper;
				this.root = this;
				this.keyName = keyName;
				this.namespacedKeyName = keyName;
				isRoot = true;
			}
			private readonly Type dataType;
			private readonly DataMapperNode parent;
			private readonly DataMapperNode root;
			private readonly bool isRoot;
			private readonly MapInstance mapper;
			private readonly string keyName;
			private readonly bool isIntermediary;
			private readonly string namespacedKeyName;
			private List<Dictionary<string, object>> data = new List<Dictionary<string, object>>();
			private int currentRow = 0;
			private readonly string _namespace;
			private readonly bool hasNamespace;
			private bool dataIndexFrozen = false;
			private readonly bool isListType;

			private DataMapperNode(Type dataType, DataMapperNode parent, string _namespace, string keyName, bool isIntermediary)
			{
				this._namespace = _namespace;
				this.mapper = parent.mapper;
				this.dataType = dataType;
				this.keyName = keyName;
				this.isIntermediary = isIntermediary;
				this.parent = parent;
				this.root = parent.root;
				this.hasNamespace = !string.IsNullOrEmpty(Namespace + _namespace);
				this.isListType = dataType.IsListType();
				namespacedKeyName = hasNamespace && keyName != null ? Namespace + "_" + keyName : keyName;
				isRoot = false;
				// sub nodes need to filter their data
				FilterData();
			}

			public bool HasData { get { return currentRow < data.Count; } }
			private string __memoizedNS = null;
			public string Namespace
			{
				get
				{
					if (__memoizedNS == null)
					{
						if (isRoot)
						{
							__memoizedNS = null;
						}
						else if (parent.hasNamespace && !string.IsNullOrWhiteSpace(_namespace))
						{
							__memoizedNS = parent.Namespace + "_" + _namespace;
						}
						else if (parent.hasNamespace)
						{
							__memoizedNS = parent.Namespace;
						}
						else if (!string.IsNullOrWhiteSpace(_namespace))
						{
							__memoizedNS = _namespace;
						}
					}
					return __memoizedNS;
				}
			}


			public object KeyValue
			{
				get
				{
					if (namespacedKeyName != null)
					{
						try
						{
							return data[currentRow][namespacedKeyName];
						}
						catch (KeyNotFoundException)
						{
							throw new SqlMapException($"Cannot find column '{namespacedKeyName}' in data set.\nAvailable columns:\n\t{string.Join("\n\t", data[currentRow].Keys)}\n");
						}
						catch (Exception)
						{
							throw new SqlMapException($"No data exists");
						}
					}
					return null;
				}
			}

			public void AddData(Dictionary<string, object> row)
			{
				if (isRoot)
				{
					data.Add(row);
				}
				else
				{
					throw new InvalidOperationException("Data can only be added to root nodes");
				}
			}

			public object Map()
			{
				if (isListType)
				{
					return MapList(dataType.ReduceList(), null);
				}
				else
				{
					return MapObject();
				}
			}

			private object MapObject()
			{
				if (!HasData)
				{
					return null;
				}

				var props = dataType.GetProperties();

				var item = Activator.CreateInstance(dataType);

				for (int i = 0; i < props.Length; i++)
				{
					var prop = props[i];
					//type of the property data in dto
					var propType = prop.PropertyType;
					//name of the prop
					string name = prop.GetSqlColumnName();

					if (!SqlIgnoreAttribute.IsIgnored(prop, SqlIgnoreFlags.Read))
					{
						if (hasNamespace)
						{
							name = Namespace + "_" + name;
						}

						if (SqlJoinAttribute.GetAttribute(prop, out var joinAttr))
						{
							List<Type> joins = joinAttr.IntermediaryJoins.ToList();
							propType = propType.ReduceList();
							if (!propType.IsSimple() && !propType.IsInterface)
							{
								// add proptype as the last table to be joined to we can complete join chain
								joins.Add(propType);
							}

							if (joins.Count == 0)
							{
								throw new SqlMapException($"Cannot use [SqlJoinAttribute] on property '{prop.Name}' without specifying at least one type in the SqlJoinAttribute IntermediaryJoins argument");
							}


							if (propType.IsInterface)
							{
								var implentationType = joins.Last();
								//see if concrete implementation actually has the interface
								if (!propType.IsAssignableFrom(implentationType))
								{
									throw new SqlMapException($"Cannot apply join on property '{propType.Name} {prop.Name}'. Type '{implentationType}' does not implement the interface '{propType.Name}'");
								}
							}

							var firstJoin = joins[0].ReduceList();
							var newNamespace = joinAttr.Alias;

							// dont filter intermediary joins if type is a list of primitives
							// only filter on the PK of the last join
							bool reduceJoinsToSingleMapper = propType.IsSimple() && prop.PropertyType.IsListType() && joins.Count > 1;
							if (reduceJoinsToSingleMapper)
							{
								newNamespace = joins.Skip(1).Aggregate(joinAttr.Alias, (ns, next) =>
												ns + "_" +
												(mapper.config.JoinNameStrategy == TableJoinNameStrategyEnum.FirstLetter ? next.Name[0].ToString() : next.Name));
								// remove all but the last join (so we can filter on that table)
								joins.RemoveRange(0, joins.Count - 1);
								firstJoin = joins[0].ReduceList();
							}

							dataIndexFrozen = true;
							string subPkName = firstJoin.GetPKColumn();

							var node = new DataMapperNode(firstJoin, this, newNamespace, subPkName, false);
							node.MapJoins(joins, prop, item);
							dataIndexFrozen = false;
						}
						else if (propType.IsSimple())
						{
							// try add the value
							SetValue(item, name, prop);
						}
						else
						{
							// is subclass type
							// freeze index so sub mapper doenst walk past this row
							dataIndexFrozen = true;
							var node = new DataMapperNode(propType, this, null, null, true);
							var subItem = node.Map();
							prop.SetValue(item, subItem);
							dataIndexFrozen = false;
						}
					}
				}

				NextRow();

				return item;
			}

			private IList MapList(Type itemType, Type listType)
			{
				var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(listType ?? itemType));
				var itemKeyName = itemType.GetPKColumn();
				var namespacedKey = hasNamespace ? Namespace + "_" + itemKeyName : itemKeyName;

				var node = new DataMapperNode(itemType, this, null, itemKeyName, false);
				while (node.HasData)
				{
					var listItem = node.Map();
					if (listItem != null)
					{
						list.Add(listItem);
					}
				}

				return list;
			}

			private object MapJoins(List<Type> remainingJoins, PropertyInfo destinationProp, object destinationItem)
			{
				// pop off front join coz we processing it now
				remainingJoins.RemoveAt(0);

				if (remainingJoins.Count > 0)
				{
					var nextType = remainingJoins[0];
					string nextKey = nextType.GetPKColumn();
					var node = new DataMapperNode(
								nextType,
								this,
								nextKey == null
									? null
									: (mapper.config.JoinNameStrategy == TableJoinNameStrategyEnum.FirstLetter ? nextType.Name[0].ToString() : nextType.Name),
								nextKey,
								false);

					return node.MapJoins(remainingJoins, destinationProp, destinationItem);
				}
				else
				{
					//no more joins do the actuall mapping
					var propType = destinationProp.PropertyType;

					if (propType.IsSimple())
					{
						//is a primitive type
						string col = Namespace + "_" + destinationProp.GetSqlColumnName();

						SetValue(destinationItem, col, destinationProp);
					}
					else if (propType.IsListType())
					{
						//is generic list. mapinto items
						var underlyingType = propType.ReduceList();

						if (underlyingType.IsSimple())
						{
							// map a list of primitives
							string col = Namespace + "_" + destinationProp.GetSqlColumnName();
							var pAdd = destinationProp.PropertyType.GetMethod("Add");
							// create an empty list to add the items to
							var list = Activator.CreateInstance(destinationProp.PropertyType);

							foreach (var row in data)
							{
								if (row.ContainsKey(col))
								{
									var val = ProcessValue(row[col], underlyingType, destinationProp);
									if (val != null)
									{
										pAdd.Invoke(list, new[] { val });
									}
								}
							}


							destinationProp.SetValue(destinationItem, list);
						}
						else
						{
							if (string.IsNullOrWhiteSpace(keyName))
							{
								throw new SqlMapException($"[SqlMappableAttribute] on type '{dataType.Name}' does not exist. Cannot map data.");
							}

							if (underlyingType.IsInterface)
							{
								//check interface
								if (!underlyingType.IsAssignableFrom(dataType))
								{
									throw new SqlMapException($"Cannot assign a type of '{dataType}' into property '{underlyingType.Name} {destinationProp.Name}'. Type '{dataType}' does not assignable to '{underlyingType.Name}'");
								}
							}

							var list = MapList(dataType, underlyingType);
							destinationProp.SetValue(destinationItem, list);
						}
					}

					else
					{
						if (string.IsNullOrWhiteSpace(keyName))
						{
							throw new SqlMapException($"[SqlMappableAttribute] on type '{dataType.Name}' does not exist. Cannot map data.");
						}

						if (propType.IsInterface)
						{
							//check interface
							if (!propType.IsAssignableFrom(dataType))
							{
								throw new SqlMapException($"Cannot assign a type of '{dataType}' into property '{propType.Name} {destinationProp.Name}'. Type '{dataType}' does not assignable to '{propType.Name}'");
							}
						}

						var itemvalue = MapObject();
						destinationProp.SetValue(destinationItem, itemvalue);
					}

					return destinationItem;
				}
			}

			private bool NextRow()
			{
				if (!dataIndexFrozen)
				{
					currentRow++;
				}

				// when there is no more data move up to the parent node and increment its row. refilter and then
				// see if there is new data
				if (!HasData)
				{
					if (parent != null && !parent.isListType && parent.HasData && !parent.dataIndexFrozen)
					{
						if (parent.NextRow())
						{
							// parent has new value reset the data index back to start so we can filter new data
							currentRow = 0;
							FilterData();
						}
					}
				}

				return HasData;
			}

			private void FilterData()
			{
				if (keyName == null || isIntermediary)
				{
					data = parent.data;
				}
				else
				{
					data = root.data.Where(x => x.ContainsKey(namespacedKeyName) && parent.IsApplicableRow(x))
						// now get unique rows for current slice
						.GroupBy(x => x[namespacedKeyName]).Select(x => x.First()).ToList();
				}
			}

			private bool IsApplicableRow(IReadOnlyDictionary<string, object> row)
			{
				if (isRoot)
				{
					// data in root is always applicable
					return true;
				}
				// check from top down
				else if (parent != null && !parent.IsApplicableRow(row))
				{
					return false;
				}
				else if (isIntermediary)
				{
					// if got this far and intermediary then its valid
					return true;
				}

				// now check this node
				object keyVal = row[namespacedKeyName];
				return HasData && keyVal.Equals(KeyValue);
			}

			private void SetValue(object item, string columnName, PropertyInfo prop)
			{
				if (data[currentRow].ContainsKey(columnName))
				{
					var raw = data[currentRow][columnName];

					prop.SetValue(item, ProcessValue(raw, prop.PropertyType, prop));
				}
			}

			private object ProcessValue(object raw, Type valueType, PropertyInfo prop)
			{
				if (raw is DBNull)
				{
					return null;
				}
				else
				{
					if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(Nullable<>))
					{
						//get tthe real type
						valueType = Nullable.GetUnderlyingType(valueType);
					}

					if (valueType.IsEnum)
					{
						if (FlagsAttribute.IsDefined(valueType, typeof(FlagsAttribute), true))
						{
							// is a flags just cast to enum
							return Enum.ToObject(valueType, raw);
						}
						else
						{
							return Enum.Parse(valueType, raw.ToString());
						}
					}
					else
					{
						try
						{
							return Convert.ChangeType(raw, valueType);
						}
						catch (InvalidCastException e)
						{
							throw new ArgumentException($"Cannot cast value of type '{raw?.GetType()}' to type '{valueType.Name}' on property '{prop.Name}' in '{dataType.Name}'. Key '{this.KeyValue}'", e);
						}
					}
				}
			}


		}


	}
}