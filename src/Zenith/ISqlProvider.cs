using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Zenith.Core;

namespace Zenith
{
	/// <summary>
	/// Interface to provide database agnostic functionality. Different databases will implement specific providers
	/// </summary>
	public interface ISqlProvider
	{
		/// <summary>
		/// Create a database specific DbConnection
		/// </summary>
		/// <returns></returns>
		DbConnection CreateConnection();
		/// <summary>
		/// Create a database specific DbCommand
		/// </summary>
		/// <returns></returns>
		DbCommand CreateCommand();
		/// <summary>
		/// Generate a sql command that will run on the specified database and will select from table `tableType`
		/// </summary>
		/// <param name="tableType"></param>
		/// <param name="alias"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		string CreateSelect(Type tableType, string alias, GenerateSelectOptions options);
		/// <summary>
		/// Generate a sql command that will run on the specified database and will insert data into table `tableType`
		/// </summary>
		/// <param name="tableType"></param>
		/// <param name="data"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		string CreateInsert(Type tableType, object data, GenerateInsertOptions options);
		/// <summary>
		/// Create a database specific paramater with the name `parameterName` and `value` as the parameter value
		/// </summary>
		/// <param name="parameterName"></param>
		/// <param name="valueType"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		DbParameter CreateParamater(string parameterName, Type valueType, object value);
		/// <summary>
		/// Takes a DbDataReader and returns an IAsyncEnumerable stream that when iterated will map data
		/// into instance(s) of `dataType`
		/// </summary>
		/// <param name="dataType"></param>
		/// <param name="reader"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		IAsyncEnumerable<object> MapStream(Type dataType, DbDataReader reader, CancellationToken token);
		/// <summary>
		/// Create and start a database specific DbTransaction
		/// </summary>
		/// <param name="connection"></param>
		/// <returns></returns>
		ValueTask<DbTransaction> CreateTransaction(DbConnection connection);
	}
}