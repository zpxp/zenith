using System;
using System.Linq;
using System.Reflection;
using Zenith.Exceptions;
using Zenith.Extensions;

namespace Zenith.Core
{
	/// <summary>
	/// Class used to generate Insert statements
	/// </summary>
	public class GenerateInsert
	{

		private readonly Config config;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="config"></param>
		public GenerateInsert(Config config = null)
		{
			this.config = config ?? new Config();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="tableType"></param>
		/// <param name="data"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public string Generate(Type tableType, object data, GenerateInsertOptions options)
		{
			options ??= new GenerateInsertOptions();
			bool hasSchema = !string.IsNullOrWhiteSpace(options.Schema);

			var mapAttr = SqlMappableAttribute.GetAttribute(tableType);
			if (options.CreateInsertInto && string.IsNullOrWhiteSpace(mapAttr?.TableName))
			{
				throw new GenerateInsertException($"[SqlMappableAttribute] on type '{tableType.Name}' does not specify TableName. Cannot create INSERT INTO.");
			}

			var props = GetApplicableColumns(tableType, data, options);

			string sql = "";

			if (options.CreateInsertInto)
			{
				sql += $"INSERT INTO {options.Schema}{(hasSchema ? config.SchemaSeparator : null)}{config.QuotePrefix + mapAttr.TableName + config.QuoteSuffix}\n";
				sql += $"({string.Join(",", props.Select(x => config.QuotePrefix + x.GetSqlColumnName() + config.QuoteSuffix))})\nVALUES\n";
			}

			sql += $"({string.Join(",", props.Select(x => config.ParameterPrefix + x.GetSqlColumnName()))})" + (options.CloseStatement ? ";" : null);

			return sql;

		}

		private static PropertyInfo[] GetApplicableColumns(Type tableType, object data, GenerateInsertOptions options)
		{
			string keyName = SqlMappableAttribute.GetAttribute(tableType, out var mapAttr) ? mapAttr.KeyName : null;
			var props = tableType.GetProperties()
					.Where(prop =>
					{
						bool valid = SqlJoinAttribute.GetAttribute(prop) == null && !SqlIgnoreAttribute.IsIgnored(prop, SqlIgnoreFlags.CreateInsert);
						if (valid && options.IgnoreEmptyProperties && data != null)
						{
							var value = prop.GetValue(data);
							// exclude nulls, empty guids and empty structs if they are the key
							if (value == null || (value is Guid g && g == Guid.Empty) || (mapAttr != null && prop.Name == keyName && value.Equals(GetDefaultValue(prop.PropertyType))))
							{
								return false;
							}
						}
						return valid;
					})
					.SelectMany(x => x.PropertyType.IsSimple() ? new PropertyInfo[] { x } : GetApplicableColumns(x.PropertyType, data, options))
					.ToArray();

			return props;
		}

		static object GetDefaultValue(Type t)
		{
			if (t.IsValueType)
			{
				return Activator.CreateInstance(t);
			}

			return null;
		}

		/// <summary>
		/// Provider based configuration for GenerateInsert
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
			/// Prefix for parameter declarations
			/// </summary>
			public virtual string ParameterPrefix { get; set; } = "@";
		}
	}

	/// <summary>
	/// Use based configuration for GenerateInsert
	/// </summary>
	public class GenerateInsertOptions
	{
		/// <summary>
		/// Add 'INSERT INTO table [column list]' at the start of the generated script
		/// </summary>
		public virtual bool CreateInsertInto { get; set; } = true;

		/// <summary>
		/// Table schema
		/// </summary>
		public virtual string Schema { get; set; } = "";

		/// <summary>
		/// Close statement with a semicolon
		/// </summary>
		public virtual bool CloseStatement { get; set; } = true;

		/// <summary>
		/// Do not insert values for empty properties. Only valid when `data` is also passed into `CreateInsert`
		/// </summary>
		public virtual bool IgnoreEmptyProperties { get; set; } = false;
	}
}