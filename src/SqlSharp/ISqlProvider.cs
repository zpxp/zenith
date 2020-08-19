using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using SqlSharp.Core;

namespace SqlSharp
{
	public interface ISqlProvider
	{
		DbConnection CreateConnection();
		DbCommand CreateCommand();
		string CreateSelect(Type tableType, string alias, GenerateSelectOptions options);
		string CreateInsert(Type tableType, object data, GenerateInsertOptions options);
		DbParameter CreateParamater(string parameterName, Type valueType, object value);
		IAsyncEnumerable<object> MapStream(Type dataType, DbDataReader reader, CancellationToken token);
		ValueTask<DbTransaction> CreateTransaction(DbConnection connection);
	}
}