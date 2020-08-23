using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Zenith;
using Zenith.Core;

namespace Zenith.Providers.SqlServer
{
	public class SqlServerProvider : ISqlProvider
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
			return new SqlCommand();
		}

		public DbConnection CreateConnection()
		{
			return new SqlConnection();
		}

		public ValueTask<DbTransaction> CreateTransaction(DbConnection connection)
		{
			return (connection as SqlConnection).BeginTransactionAsync();
		}

		public DbParameter CreateParamater(string parameterName, Type valueType, object value)
		{
			return new SqlParameter(parameterName, value);
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