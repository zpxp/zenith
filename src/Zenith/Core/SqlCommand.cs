using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Zenith.Extensions;
using Zenith.Utility;

namespace Zenith.Core
{
	internal class SqlCommand : ISqlCommand
	{
		private readonly SqlTypeEnum type;
		private readonly string sql;
		private readonly UnitOfWork unitOfWork;
		private readonly SqlConfiguration config;

		private List<SingleParameter> singleParameters = new List<SingleParameter>();
		private List<ObjectParameter> objectParameters = new List<ObjectParameter>();
		private Dictionary<string, object> middlewareSwitches = new Dictionary<string, object>();

		public SqlCommand(SqlTypeEnum type, string sql, UnitOfWork unitOfWork)
		{
			this.config = unitOfWork.Config;
			this.unitOfWork = unitOfWork;
			this.sql = sql;
			this.type = type;
		}



		public void AddArgument<T>(string parameterName, T value)
		{
			singleParameters.Add(new SingleParameter { ParameterName = parameterName, Data = value, DataType = typeof(T) });
		}

		public void AddArgument(string parameterName, Type valueType, object value)
		{
			singleParameters.Add(new SingleParameter { ParameterName = parameterName, Data = value, DataType = valueType });
		}


		public void AddArguments(object data)
		{
			objectParameters.Add(new ObjectParameter { Data = data, DataType = data.GetType() });
		}

		public void AddArguments(object data, Func<PropertyInfo, object, bool> predicate)
		{
			objectParameters.Add(new ObjectParameter { Data = data, DataType = data.GetType(), Predicate = predicate });
		}

		public void AddSwitch(string key, object data = null)
		{
			middlewareSwitches.Add(key, data);
		}

		public void AddSwitch(Enum key, object data = null)
		{
			middlewareSwitches.Add(key.ToString(), data);
		}

		public async Task<object> SelectSingleAsync(Type dataType, CancellationToken token = default)
		{
			using var canceller = new TokenLink(token);
			var stream = await WithMiddleware(SelectStreamImpl, dataType, null, canceller.Token).ConfigureAwait(false);
			return await stream.FirstOrDefaultAsync().ConfigureAwait(false);
		}

		public async Task<T> SelectSingleAsync<T>(CancellationToken token = default)
		{
			using var canceller = new TokenLink(token);
			var stream = await WithMiddleware(SelectStreamImpl, typeof(T), null, canceller.Token).ConfigureAwait(false);
			var result = (T)await stream.FirstOrDefaultAsync().ConfigureAwait(false);
			return result;
		}

		public async IAsyncEnumerable<object> SelectManyStreamAsync(Type dataType, [EnumeratorCancellation] CancellationToken token = default)
		{
			using var canceller = new TokenLink(token);
			var stream = await WithMiddleware(SelectStreamImpl, dataType, null, canceller.Token).ConfigureAwait(false);
			await foreach (var row in stream.WithCancellation(token))
			{
				yield return row;
			}
		}


		public async IAsyncEnumerable<T> SelectManyStreamAsync<T>([EnumeratorCancellation] CancellationToken token = default)
		{
			using var canceller = new TokenLink(token);
			var stream = await WithMiddleware(SelectStreamImpl, typeof(T), null, canceller.Token);
			await foreach (var row in stream.WithCancellation(token).ConfigureAwait(false))
			{
				yield return (T)row;
			}
		}

		public async Task<List<object>> SelectManyAsync(Type dataType, CancellationToken token = default)
		{
			using var canceller = new TokenLink(token);
			var stream = await WithMiddleware(SelectStreamImpl, dataType, null, canceller.Token).ConfigureAwait(false);
			// read to end of stream then return a list
			return await stream.ToListAsync().ConfigureAwait(false);
		}

		public async Task<List<T>> SelectManyAsync<T>(CancellationToken token = default)
		{
			using var canceller = new TokenLink(token);
			var stream = await WithMiddleware(SelectStreamImpl, typeof(T), null, canceller.Token).ConfigureAwait(false);
			// read to end of stream then return a list
			return await stream.Cast<T>().ToListAsync().ConfigureAwait(false);
		}

		public async Task<List<object>> SelectManyAsync(Type dataType, string columnName, CancellationToken token = default)
		{
			using var canceller = new TokenLink(token);
			var stream = await WithMiddleware(SelectStreamImpl, dataType, columnName, canceller.Token).ConfigureAwait(false);
			// read to end of stream then return a list
			return await stream.ToListAsync().ConfigureAwait(false);
		}

		public async Task<List<T>> SelectManyAsync<T>(string columnName, CancellationToken token = default)
		{
			using var canceller = new TokenLink(token);
			var stream = await WithMiddleware(SelectStreamImpl, typeof(T), columnName, canceller.Token).ConfigureAwait(false);
			// read to end of stream then return a list
			return await stream.Cast<T>().ToListAsync().ConfigureAwait(false);
		}

		public async IAsyncEnumerable<object> SelectManyStreamAsync(Type dataType, string columnName, [EnumeratorCancellation] CancellationToken token = default)
		{
			using var canceller = new TokenLink(token);
			var stream = await WithMiddleware(SelectStreamImpl, dataType, columnName, canceller.Token).ConfigureAwait(false);
			await foreach (var row in stream.WithCancellation(token).ConfigureAwait(false))
			{
				yield return row;
			}
		}

		public async IAsyncEnumerable<T> SelectManyStreamAsync<T>(string columnName, [EnumeratorCancellation] CancellationToken token = default)
		{
			using var canceller = new TokenLink(token);
			var stream = await WithMiddleware(SelectStreamImpl, typeof(T), columnName, canceller.Token).ConfigureAwait(false);
			await foreach (var row in stream.WithCancellation(token).ConfigureAwait(false))
			{
				yield return (T)row;
			}
		}

		public async Task<T> SelectSingleAsync<T>(string columnName, CancellationToken token = default)
		{
			using var canceller = new TokenLink(token);
			var stream = await WithMiddleware(SelectStreamImpl, typeof(T), columnName, canceller.Token).ConfigureAwait(false);
			var result = (T)await stream.FirstOrDefaultAsync().ConfigureAwait(false);
			return result;
		}

		public async Task<object> SelectSingleAsync(Type dataType, string columnName, CancellationToken token = default)
		{
			using var canceller = new TokenLink(token);
			var stream = await WithMiddleware(SelectStreamImpl, dataType, columnName, canceller.Token).ConfigureAwait(false);
			var result = await stream.FirstOrDefaultAsync().ConfigureAwait(false);
			return result;
		}

		public async Task ExecuteAsync(CancellationToken token = default)
		{
			using var canceller = new TokenLink(token);
			var stream = await WithMiddleware(ExecuteImpl, null, null, canceller.Token).ConfigureAwait(false);
			await stream.GetAsyncEnumerator().MoveNextAsync().ConfigureAwait(false);
		}



		public async Task<DbDataReader> SelectRawAsync()
		{
			var stream = await WithMiddleware(SelectRawImpl, typeof(DbDataReader), null, default).ConfigureAwait(false);
			// the consoomer must dispose the reader
			return (DbDataReader)await stream.FirstAsync().ConfigureAwait(false);
		}

		private IEnumerable<Func<CommandContext, Func<Task<IAsyncEnumerable<object>>>, Task<IAsyncEnumerable<object>>>> EnumerateMiddleware()
		{
			// a generator to remember the current pos of the middleware chain
			for (int i = 0; i < config.middlewares.Count; i++)
			{
				yield return config.middlewares[i];
			}
		}

		private void ProcessObjectParameter(DbCommand command, object data, Type dataType, Func<PropertyInfo, object, bool> predicate)
		{
			foreach (var prop in dataType.GetProperties().Where(x => SqlJoinAttribute.GetAttribute(x) == null && !SqlIgnoreAttribute.IsIgnored(x, SqlIgnoreFlags.AddArguments)))
			{
				Type propType = prop.PropertyType;
				if (propType.IsSimple())
				{
					object propData = prop.GetValue(data);
					Type rnpropType = propType.ReduceNullable();
					if (predicate != null && !predicate.Invoke(prop, propData))
					{
						// ignore this prop
						continue;
					}

					if (rnpropType.IsEnum)
					{
						if (FlagsAttribute.IsDefined(rnpropType, typeof(FlagsAttribute), true))
						{
							propType = typeof(int);
						}
						else
						{
							propData = propData?.ToString();
							propType = typeof(string);
						}
					}

					string name = prop.GetSqlColumnName();
					var param = config.Provider.CreateParamater(name, propType, propData);
					command.Parameters.Add(param);
				}
				else
				{
					// handle sub class props
					object propData = prop.GetValue(data);
					ProcessObjectParameter(command, propData, propType, predicate);
				}
			}
		}

		private void ProcessParameters(DbCommand command, CommandContext context)
		{
			foreach (var item in context.ObjectParameters)
			{
				ProcessObjectParameter(command, item.Data, item.DataType, item.Predicate);
			}

			foreach (var item in context.SingleParameters)
			{
				object data = item.Data;
				if (item.DataType.IsEnum && !FlagsAttribute.IsDefined(item.DataType, typeof(FlagsAttribute), true))
				{
					data = data?.ToString();
				}
				var param = config.Provider.CreateParamater(item.ParameterName, item.DataType, data);
				command.Parameters.Add(param);
			}
		}


		private async Task<IAsyncEnumerable<object>> WithMiddleware(Func<DbCommand, Type, string, CancellationToken, IAsyncEnumerable<object>> action, Type dataType, string columnName, CancellationToken token)
		{
			var command = config.Provider.CreateCommand();
			command.Connection = await unitOfWork.GetConnectionAsync(this.type).ConfigureAwait(false);
			command.Transaction = unitOfWork.Transaction;
			var context = new CommandContext
			{
				Sql = sql,
				Type = type,
				SingleParameters = singleParameters,
				ObjectParameters = objectParameters,
				MiddlewareSwitches = middlewareSwitches,
				Container = unitOfWork.scope
			};

			if (config.middlewares.Count == 0)
			{
				// no middleware. create command and execute
				command.CommandText = context.Sql;
				ProcessParameters(command, context);
				return action(command, dataType, columnName, token);
			}
			else
			{
				var enumerator = EnumerateMiddleware().GetEnumerator();
				enumerator.MoveNext();

				Task<IAsyncEnumerable<object>> Next()
				{
					// move to next middleware and invoke it otherwise run the actual command
					if (enumerator.MoveNext())
					{
						return enumerator.Current(context, Next);
					}
					else
					{
						command.CommandText = context.Sql;
						ProcessParameters(command, context);
						return Task.FromResult(action(command, dataType, columnName, token));
					}
				};

				return await enumerator.Current(context, Next).ConfigureAwait(false);
			}
		}


		private async IAsyncEnumerable<object> SelectStreamImpl(DbCommand command, Type dataType, string columnName, [EnumeratorCancellation] CancellationToken token)
		{
			using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
			{
				if (reader.HasRows)
				{
					if (columnName == null)
					{
						// use the mapper
						await foreach (var row in config.Provider.MapStream(dataType, reader, token).ConfigureAwait(false))
						{
							yield return row;
						}
					}
					else
					{
						// just read that column directly
						while (await reader.ReadAsync().ConfigureAwait(false))
						{
							var raw = reader[columnName];

							if (raw is DBNull)
							{
								if (dataType.IsValueType)
								{
									yield return Activator.CreateInstance(dataType);
								}
								else
								{
									yield return null;
								}
							}
							else
							{
								if (dataType.IsEnum)
								{
									yield return Enum.Parse(dataType, raw.ToString());
								}
								else
								{
									yield return raw;
								}
							}
						}
					}
				}
			}
			yield break;
		}

		private async IAsyncEnumerable<object> ExecuteImpl(DbCommand command, Type dataType, string columnName, [EnumeratorCancellation] CancellationToken token)
		{
			yield return await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
		}

		private async IAsyncEnumerable<DbDataReader> SelectRawImpl(DbCommand command, Type dataType, string columnName, [EnumeratorCancellation] CancellationToken token)
		{
			// the reader must be disposed by the consoomer
			var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
			yield return reader;
		}

		public void Dispose()
		{
			unitOfWork.commands.Remove(this);
		}


	}
}