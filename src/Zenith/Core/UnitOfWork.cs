using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Zenith.Core;

namespace Zenith.Core
{
	internal class UnitOfWork : IUnitOfWork
	{
		private readonly SqlConfiguration config;
		internal readonly IServiceProvider scope;
		private DbConnection connection;
		internal DbTransaction Transaction { get; private set; }

		internal List<SqlCommand> commands = new List<SqlCommand>();

		public SqlConfiguration Config => config;
		private readonly ILogger logger;

		public UnitOfWork(SqlConfiguration config, IServiceProvider scope, ILogger<UnitOfWork> logger)
		{
			this.logger = logger;
			this.scope = scope;
			this.config = config;
		}

		public string CreateSelect<TTable>(string alias, GenerateSelectOptions options = null)
		{
			return config.Provider.CreateSelect(typeof(TTable), alias, options);
		}

		public string CreateSelect(Type tableType, string alias, GenerateSelectOptions options = null)
		{
			return config.Provider.CreateSelect(tableType, alias, options);
		}

		public string CreateInsert<TTable>(GenerateInsertOptions options = null)
		{
			return config.Provider.CreateInsert(typeof(TTable), null, options);
		}

		public string CreateInsert(Type tableType, GenerateInsertOptions options = null)
		{
			return config.Provider.CreateInsert(tableType, null, options);
		}

		public string CreateInsert<TTable>(TTable data, GenerateInsertOptions options = null)
		{
			return config.Provider.CreateInsert(typeof(TTable), data, options);
		}

		public string CreateInsert(Type tableType, object data, GenerateInsertOptions options = null)
		{
			return config.Provider.CreateInsert(tableType, data, options);
		}


		public ISqlCommand NewCommand(SqlTypeEnum type, string sql)
		{
			var command = new SqlCommand(type, sql, this);
			commands.Add(command);
			return command;
		}

		internal async Task<DbConnection> GetConnectionAsync(SqlTypeEnum type)
		{
			if (connection != null && connection.State != ConnectionState.Broken && connection.State != ConnectionState.Closed)
			{
				// we good
				await CreateTransaction(type);
				return connection;
			}
			else if (connection != null)
			{
				// clear shit connection object
				ClearConnection();
			}

			connection = Config.Provider.CreateConnection();
			connection.ConnectionString = config.ConnectionString;
			await connection.OpenAsync();
			await CreateTransaction(type);
			return connection;
		}

		private async Task CreateTransaction(SqlTypeEnum type)
		{
			if (Transaction == null)
			{
				switch (type)
				{
					case SqlTypeEnum.Unknown:
					case SqlTypeEnum.Update:
					case SqlTypeEnum.Delete:
					case SqlTypeEnum.Insert:
					default:
						Transaction = await config.Provider.CreateTransaction(connection);
						break;
					case SqlTypeEnum.Select:
						// no transaction needed
						break;
				}
			}
		}

		public async Task CommitAsync(CancellationToken token = default)
		{
			if (Transaction != null)
			{
				await Transaction.CommitAsync(token);
				Transaction = null;
			}
		}

		public async Task RollbackAsync(CancellationToken token = default)
		{
			if (Transaction != null)
			{
				await Transaction.RollbackAsync(token);
			}
		}

		internal void ClearConnection()
		{
			if (connection != null)
			{
				connection.Dispose();
				connection = null;
			}
		}

		public void Dispose()
		{
			// use rfor loop coz dispose will remove items from commands list
			for (int i = commands.Count - 1; i >= 0; i--)
			{
				commands[i].Dispose();
			}
			if (Transaction != null)
			{
				try
				{
					Transaction.Rollback();
				}
				catch (Exception e)
				{
					logger.LogError(e, "IUnitOfWork");
				}
				Transaction.Dispose();
				Transaction = null;
			}
			ClearConnection();
		}
	}
}