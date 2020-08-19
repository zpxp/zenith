using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using SqlSharp;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace SqlSharpTest
{

	public class UnitTestUpdate : IClassFixture<TestsFixture>
	{
		private readonly TestsFixture data;
		public UnitTestUpdate(TestsFixture data)
		{
			this.data = data;
		}



		[Fact]
		public async Task Update()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string sql = @"
UPDATE [Contact]
SET [Number] = @number
WHERE [ContactId] = 3;
				";

				string number = "12312321312312";
				using var command = unitOfWork.NewCommand(SqlTypeEnum.Update, sql);
				command.AddArgument("number", number);
				await command.ExecuteAsync();
				await unitOfWork.CommitAsync();

				sql = @"
SELECT [Number] FROM [Contact]
WHERE [ContactId] = 3;
";
				using var command2 = unitOfWork.NewCommand(SqlTypeEnum.Select, sql);
				var dbnumber = await command2.SelectSingleAsync<string>("Number");
				Assert.Equal(number, dbnumber);
			}
		}


		[Fact]
		public async Task UpdateTransactionTest()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var scope1 = processContainer.CreateScope();
				var unitOfWork1 = scope1.ServiceProvider.GetRequiredService<IUnitOfWork>();
				string sql = @"
UPDATE [Contact]
SET [Number] = @number
WHERE [ContactId] = 3;
				";

				string number = "878789";
				using var command = unitOfWork1.NewCommand(SqlTypeEnum.Update, sql);
				command.AddArgument("number", number);
				await command.ExecuteAsync();

				var scope2 = processContainer.CreateScope();
				var unitOfWork2 = scope2.ServiceProvider.GetRequiredService<IUnitOfWork>();

				sql = @"
SELECT [Number] FROM [Contact]
WHERE [ContactId] = 3;
";
				using var command2 = unitOfWork2.NewCommand(SqlTypeEnum.Select, sql);
				var dbnumber = await command2.SelectSingleAsync<string>("Number");
				Assert.NotEqual(number, dbnumber);

				await unitOfWork1.CommitAsync();
				//should be equal now
				dbnumber = await command2.SelectSingleAsync<string>("Number");
				Assert.Equal(number, dbnumber);
			}
		}


		[Fact]
		public async Task AutoRollback()
		{
			try
			{
				using (var processContainer = data.services.BuildServiceProvider())
				{
					var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
					string sql = @"
UPDATE [Contact]
SET [Number] = @number
WHERE [ContactId] = 1;
				";

					string number = "12312321312312";
					using var command = unitOfWork.NewCommand(SqlTypeEnum.Update, sql);
					command.AddArgument("number", number);
					await command.ExecuteAsync();
					// throw without commiting
					throw new Exception();
				}
			}
			catch (Exception) { }


			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();

				string sql = @"
SELECT [Number] FROM [Contact]
WHERE [ContactId] = 1;
";
				using var command2 = unitOfWork.NewCommand(SqlTypeEnum.Select, sql);
				var dbnumber = await command2.SelectSingleAsync<string>("Number");
				// should not have changed
				Assert.Equal("123", dbnumber);
			}
		}

		[Fact]
		public async Task AutoRollback2()
		{
			try
			{
				using (var processContainer = data.services.BuildServiceProvider())
				{
					var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
					string sql = @"
UPDATE [Contact]
SET [Number] = @number
WHERE [ContactId] = 1;
				";

					string number = "12312321312312";
					using var command = unitOfWork.NewCommand(SqlTypeEnum.Update, sql);
					command.AddArgument("number", number);
					await command.ExecuteAsync();
					await unitOfWork.CommitAsync();
					// throw after commiting
					throw new Exception();
				}
			}
			catch (Exception) { }


			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();

				string sql = @"
SELECT [Number] FROM [Contact]
WHERE [ContactId] = 1;
";
				using var command2 = unitOfWork.NewCommand(SqlTypeEnum.Select, sql);
				var dbnumber = await command2.SelectSingleAsync<string>("Number");
				// should have changed
				Assert.Equal("12312321312312", dbnumber);
			}
		}


		[Fact]
		public async Task UpdateMiddleware()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string sql = @"
UPDATE [Worker]
SET [Name] = @Name
	,[UpdatedOn] = @UpdatedOn
WHERE [WorkerId] = @WorkerId;
				";

				BaseWorker row = new BaseWorker
				{
					WorkerId = 2,
					WorkerName = "Wroker 2 name boiz"
				};

				using var command = unitOfWork.NewCommand(SqlTypeEnum.Update, sql);
				command.AddArguments(row);
				await command.ExecuteAsync();
				await unitOfWork.CommitAsync();

				Assert.NotNull(row.UpdatedOn);
			}

			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string sql = unitOfWork.CreateSelect<BaseWorker>("w");
				sql += @"
WHERE w.[WorkerId] = 2;
";
				using var command = unitOfWork.NewCommand(SqlTypeEnum.Select, sql);
				var row = await command.SelectSingleAsync<BaseWorker>();
				// should have changed
				Assert.NotNull(row.UpdatedOn);
				Assert.Equal("Wroker 2 name boiz", row.WorkerName);
			}
		}

	}
}
