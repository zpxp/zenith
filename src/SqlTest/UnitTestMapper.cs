using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using SqlSharp;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace SqlSharpTest
{

	public class UnitTestMapper : IClassFixture<TestsFixture>
	{
		private readonly TestsFixture data;
		public UnitTestMapper(TestsFixture data)
		{
			this.data = data;
		}



		[Fact]
		public async Task SelectSingle()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string sql = unitOfWork.CreateSelect<BaseBossman>("b");
				using var command = unitOfWork.NewCommand(SqlTypeEnum.Select, sql);
				var row = await command.SelectSingleAsync<BaseBossman>();
				Assert.NotNull(row);
				Assert.Equal(string.IsNullOrWhiteSpace(row.Name), false);
			}
		}


		[Fact]
		public async Task SelectList()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string sql = unitOfWork.CreateSelect<BaseBossman>("b");
				sql += @"ORDER BY b.BossmanId ASC";

				using var command = unitOfWork.NewCommand(SqlTypeEnum.Select, sql);
				var rows = await command.SelectManyAsync<BaseBossman>();
				Assert.NotNull(rows);
				Assert.Equal(rows.Count, 2);
				for (int i = 0; i < rows.Count; i++)
				{
					Assert.Equal(rows[i].BossmanId, i + 1);
				}
			}
		}

		[Fact]
		public async Task SelectListDesc()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string sql = unitOfWork.CreateSelect<BaseBossman>("b");
				sql += @"ORDER BY b.BossmanId DESC";

				using var command = unitOfWork.NewCommand(SqlTypeEnum.Select, sql);
				var rows = await command.SelectManyAsync<BaseBossman>();
				Assert.NotNull(rows);
				Assert.Equal(rows.Count, 2);
				Assert.Equal(rows[0].BossmanId, 2);
				Assert.Equal(rows[1].BossmanId, 1);
			}
		}

		[Fact]
		public async Task SelectListStream()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string sql = unitOfWork.CreateSelect<BaseBossman>("b");
				sql += @"ORDER BY b.BossmanId DESC";

				using var command = unitOfWork.NewCommand(SqlTypeEnum.Select, sql);
				var stream = command.SelectManyStreamAsync<BaseBossman>();
				await foreach (var row in stream)
				{
					Assert.NotNull(row);
					Assert.Equal(row.BossmanId, 2);
					break;
				}

				await foreach (var row in stream)
				{
					Assert.NotNull(row);
					Assert.Equal(row.BossmanId, 2);
					break;
				}
			}
		}



		[Fact]
		public async Task SelectSingleJoins()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string sql = unitOfWork.CreateSelect<BossmanEzJoin>("b");
				using var command = unitOfWork.NewCommand(SqlTypeEnum.Select, sql);
				var row = await command.SelectSingleAsync<BossmanEzJoin>();
				Assert.NotNull(row);
				Assert.Equal(row.Buildings.Count, 2);
				Assert.Equal(row.Buildings[0].BossmanBuildingId, 1);
				Assert.Equal(row.Buildings[1].BossmanBuildingId, 3);
			}
		}

		[Fact]
		public async Task SelectManyJoins()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string sql = unitOfWork.CreateSelect<BossmanEzJoin>("b");
				using var command = unitOfWork.NewCommand(SqlTypeEnum.Select, sql);
				var rows = await command.SelectManyAsync<BossmanEzJoin>();
				Assert.NotNull(rows);
				Assert.Equal(rows.Count, 2);
				Assert.Equal(rows[0].Buildings.Count, 2);
				Assert.Equal(rows[0].Buildings[0].BossmanBuildingId, 1);
				Assert.Equal(rows[0].Buildings[1].BossmanBuildingId, 3);

				Assert.Equal(rows[1].Buildings.Count, 1);
				Assert.Equal(rows[1].Buildings[0].BossmanBuildingId, 2);
			}
		}


		[Fact]
		public async Task SelectSingleMegaJoins()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string sql = unitOfWork.CreateSelect<BuildingMegaJoins>("b");
				using var command = unitOfWork.NewCommand(SqlTypeEnum.Select, sql);
				var row = await command.SelectSingleAsync<BuildingMegaJoins>();
				Assert.NotNull(row);
				// this will be 2 bcoz of SqlJoin condition on Cars
				Assert.Equal(row.Cars.Count, 2);
				Assert.Equal(row.Bossmans.SelectMany(x => x.Workers.SelectMany(f => f.Cars)).Count(), 5);
				Assert.Equal(row.Bossmans.SelectMany(x => x.Workers.SelectMany(f => f.Cars)).Count(), 5);
				Assert.Equal(row.Bossmans.Select(x => x.BossmanId).Distinct().Count(), 2);
				Assert.Equal(row.Bossmans.SelectMany(x => x.Workers.Select(v => v.WorkerId)).Distinct().Count(), 4);
			}
		}

		[Fact]
		public async Task SelectSingleMegaJoinsNoCon()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string sql = unitOfWork.CreateSelect<BuildingMegaJoinsNoCon>("b");
				using var command = unitOfWork.NewCommand(SqlTypeEnum.Select, sql);
				var row = await command.SelectSingleAsync<BuildingMegaJoinsNoCon>();
				Assert.NotNull(row);
				// no condition here should be 5
				Assert.Equal(row.Cars.Count, 5);
				Assert.Equal(row.Bossmans.SelectMany(x => x.Workers.SelectMany(f => f.Cars)).Count(), 5);
				Assert.Equal(row.Bossmans.Select(x => x.BossmanId).Distinct().Count(), 2);
				Assert.Equal(row.Bossmans.SelectMany(x => x.Workers.Select(v => v.WorkerId)).Distinct().Count(), 4);
			}
		}



		[Fact]
		public async Task SubclassMapping()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string sql = unitOfWork.CreateSelect<BossmanBuildingSubClass>("b");
				sql += @"
WHERE b.buildingId = 2
";
				using var command = unitOfWork.NewCommand(SqlTypeEnum.Select, sql);
				var row = await command.SelectSingleAsync<BossmanBuildingSubClass>();
				Assert.NotNull(row);
				Assert.Equal(row.Subprops.BossmanId, 1);
				Assert.Equal(row.Subprops.BuildingId, 2);
			}
		}


		[Fact]
		public async Task InterfaceMapping()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string sql = unitOfWork.CreateSelect<WorkerInterface>("w");
				sql += @"
WHERE w.workerId = 1
";
				using var command = unitOfWork.NewCommand(SqlTypeEnum.Select, sql);
				var row = await command.SelectSingleAsync<WorkerInterface>();
				Assert.NotNull(row);
				Assert.Equal(row.Cars.Count, 2);
			}
		}


	}
}
