using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using Npgsql;
using System.Threading.Tasks;
using Sql;
using Sql.Extensions;
using Sql.Core;

namespace Postgresql.Profile
{
	public class PostgresqlProvider : ISqlProvider
	{

		private readonly GenerateSelect selectBuilder;
		private readonly GenerateInsert insertBuilder;
		private readonly DataMapper mapper;

		public PostgresqlProvider(GenerateSelect.Config selectBuilderConfig = null, GenerateInsert.Config insertBuilderConfig = null, DataMapper.Config mapperConfig = null)
		{
			this.selectBuilder = new GenerateSelect(selectBuilderConfig ?? new GenerateSelect.Config
			{
				QuotePrefix = "",
				QuoteSuffix = "",
				IdentifierNameComparer = StringComparer.OrdinalIgnoreCase,
			});
			this.insertBuilder = new GenerateInsert(insertBuilderConfig ?? new GenerateInsert.Config
			{
				QuotePrefix = "",
				QuoteSuffix = "",
				ParameterPrefix = "@"
			});
			this.mapper = new DataMapper(mapperConfig ?? new DataMapper.Config
			{
				IdentifierNameComparer = StringComparer.OrdinalIgnoreCase
			});
		}

		public DbCommand CreateCommand()
		{
			return new NpgsqlCommand();
		}

		public DbConnection CreateConnection()
		{
			return new NpgsqlConnection();
		}

		public async ValueTask<DbTransaction> CreateTransaction(DbConnection connection)
		{
			return await (connection as NpgsqlConnection).BeginTransactionAsync();
		}

		public DbParameter CreateParamater(string parameterName, Type valueType, object value)
		{
			if (value != null)
			{
				valueType = valueType.ReduceNullable();
				return (DbParameter)typeof(Npgsql.NpgsqlParameter<>)
						.MakeGenericType(valueType)
						.GetConstructor(new Type[] { typeof(string), valueType })
						.Invoke(new[] { parameterName, value });
			}
			else if (valueType.IsSimple())
			{
				return (DbParameter)typeof(Npgsql.NpgsqlParameter<>)
						.MakeGenericType(valueType)
						.GetConstructor(new Type[] { typeof(string), valueType })
						.Invoke(new[] { parameterName, value });
			}
			else
			{
				throw new InvalidOperationException($"Cannot create a NpgsqlParameter for type {valueType.Name}");
			}
		}

		public string CreateInsert(Type tableType, object data, GenerateInsertOptions options)
		{
			return insertBuilder.Generate(tableType, data, options);
		}

		public string CreateSelect(Type tableType, string alias, GenerateSelectOptions options)
		{
			return selectBuilder.Generate(tableType, alias, options);
		}

		public IAsyncEnumerable<object> MapStream(Type dataType, DbDataReader reader, CancellationToken token)
		{
			return mapper.MapStream(dataType, reader, token);
		}

	}
}