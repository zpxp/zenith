using System;
using System.Threading;
using System.Threading.Tasks;
using Zenith.Core;

namespace Zenith
{
	public interface IUnitOfWork : IDisposable
	{
		ISqlCommand NewCommand(SqlTypeEnum type, string sql);
		string CreateInsert<TTable>(GenerateInsertOptions options = null);
		string CreateInsert(Type tableType, GenerateInsertOptions options = null);
		string CreateInsert<TTable>(TTable data, GenerateInsertOptions options = null);
		string CreateInsert(Type tableType, object data, GenerateInsertOptions options = null);
		string CreateSelect<TTable>(string alias, GenerateSelectOptions options = null);
		string CreateSelect(Type tableType, string alias, GenerateSelectOptions options = null);
		Task CommitAsync(CancellationToken token = default);
		Task RollbackAsync(CancellationToken token = default);
	}
}