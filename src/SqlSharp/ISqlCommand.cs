using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SqlSharp
{
	public interface ISqlCommand : IDisposable
	{
		void AddArgument<T>(string parameterName, T value);
		void AddArgument(string parameterName, Type valueType, object value);
		void AddArguments(object data);
		void AddArguments(object data, Func<PropertyInfo, object, bool> predicate);
		Task<List<object>> SelectManyAsync(Type dataType, CancellationToken token = default);
		Task<List<object>> SelectManyAsync(Type dataType, string columnName, CancellationToken token = default);
		Task<List<T>> SelectManyAsync<T>(CancellationToken token = default);
		Task<List<T>> SelectManyAsync<T>(string columnName, CancellationToken token = default);
		IAsyncEnumerable<object> SelectManyStreamAsync(Type dataType, CancellationToken token = default);
		IAsyncEnumerable<object> SelectManyStreamAsync(Type dataType, string columnName, CancellationToken token = default);
		IAsyncEnumerable<T> SelectManyStreamAsync<T>(CancellationToken token = default);
		IAsyncEnumerable<T> SelectManyStreamAsync<T>(string columnName, CancellationToken token = default);
		Task<DbDataReader> SelectRawAsync();
		Task ExecuteAsync(CancellationToken token = default);
		Task<T> SelectSingleAsync<T>(CancellationToken token = default);
		Task<T> SelectSingleAsync<T>(string columnName, CancellationToken token = default);
		Task<object> SelectSingleAsync(Type dataType, CancellationToken token = default);
		Task<object> SelectSingleAsync(Type dataType, string columnName, CancellationToken token = default);
		void AddSwitch(string key, object data = null);
		void AddSwitch(Enum key, object data = null);
	}
}