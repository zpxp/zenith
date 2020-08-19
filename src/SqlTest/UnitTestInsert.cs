using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using SqlSharp;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace SqlSharpTest
{

	public class UnitTestInsert : IClassFixture<TestsFixture>
	{
		private readonly TestsFixture data;
		public UnitTestInsert(TestsFixture data)
		{
			this.data = data;
		}

		[Fact]
		public void GenInsert()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string expect = "INSERT INTO [Bossman]\n([BossmanId],[ContactId],[Name])\nVALUES\n(@BossmanId,@ContactId,@Name);";
				string sql = unitOfWork.CreateInsert<BaseBossman>();
				Assert.Equal(expect, sql);
			}
		}


		[Fact]
		public void GenInsert2()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string expect = "INSERT INTO [Worker]\n([WorkerId],[ContactId],[Name],[UpdatedOn])\nVALUES\n(@WorkerId,@ContactId,@Name,@UpdatedOn);";
				string sql = unitOfWork.CreateInsert<BaseWorker>();
				Assert.Equal(expect, sql);
			}
		}



		[Fact]
		public void GenInsert3()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string expect = "INSERT INTO [Building]\n([BuildingId],[Name])\nVALUES\n(@BuildingId,@Name);";
				string sql = unitOfWork.CreateInsert<BuildingMegaJoinsNoCon>();
				Assert.Equal(expect, sql);
			}
		}


		[Fact]
		public async Task GenInsertSubClass()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string expect = "INSERT INTO [BossmanBuilding]\n([BossmanBuildingId],[BuildingId],[BossmanId])\nVALUES\n(@BossmanBuildingId,@BuildingId,@BossmanId);";
				string sql = unitOfWork.CreateInsert<BossmanBuildingSubClass>();
				Assert.Equal(expect, sql);
				var row = new BossmanBuildingSubClass
				{
					BossmanBuildingId = 7,
					Subprops = new BossmanBuildingSubClass.Sub
					{
						BuildingId = 1,
						BossmanId = 1
					}
				};

				using var command = unitOfWork.NewCommand(SqlTypeEnum.Insert, sql);
				command.AddArguments(row);
				await command.ExecuteAsync();
				await unitOfWork.CommitAsync();
			}


			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string sql = unitOfWork.CreateSelect<BossmanBuildingSubClass>("b");
				sql += @"WHERE b.BossmanBuildingId = 7";
				using var command = unitOfWork.NewCommand(SqlTypeEnum.Select, sql);
				var row = await command.SelectSingleAsync<BossmanBuildingSubClass>();
				Assert.NotNull(row);
				Assert.Equal(1, row.Subprops.BossmanId);
			}
		}



		[Fact]
		public async Task GenInsertExec()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string sql = unitOfWork.CreateInsert<BaseWorker>();
				var row = new BaseWorker
				{
					WorkerId = 99,
					WorkerName = "mad dog"
				};
				using var command = unitOfWork.NewCommand(SqlTypeEnum.Insert, sql);
				command.AddArguments(row);
				await command.ExecuteAsync();
				Assert.NotNull(row.UpdatedOn);
				await unitOfWork.CommitAsync();
			}

			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string sql = unitOfWork.CreateSelect<BaseWorker>("w");
				sql += @"WHERE w.workerid = 99";
				using var command = unitOfWork.NewCommand(SqlTypeEnum.Select, sql);
				var row = await command.SelectSingleAsync<BaseWorker>();
				Assert.NotNull(row);
				Assert.Equal("mad dog", row.WorkerName);
			}
		}

	}
}
