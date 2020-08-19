using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using SqlSharp;
using SqlSharp.Core;

namespace SqlSharp.Providers.SQLite
{
	public class SQLiteProvider : ISqlProvider
	{
		private readonly GenerateSelect selectBuilder = new GenerateSelect(new GenerateSelect.Config
		{
			QuotePrefix = "[",
			QuoteSuffix = "]",
			IdentifierNameComparer = StringComparer.OrdinalIgnoreCase
		});
		private readonly GenerateInsert insertBuilder = new GenerateInsert(new GenerateInsert.Config
		{
			QuotePrefix = "[",
			QuoteSuffix = "]",
			ParameterPrefix = "@"
		});
		private readonly DataMapper mapper = new DataMapper(new DataMapper.Config
		{
			IdentifierNameComparer = StringComparer.OrdinalIgnoreCase
		});

		public DbCommand CreateCommand()
		{
			return new SQLiteCommand();
		}

		public DbConnection CreateConnection()
		{
			return new SQLiteConnection();
		}

		public ValueTask<DbTransaction> CreateTransaction(DbConnection connection)
		{
			return (connection as SQLiteConnection).BeginTransactionAsync();
		}

		public DbParameter CreateParamater(string parameterName, Type valueType, object value)
		{
			return new SQLiteParameter(parameterName, value);
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