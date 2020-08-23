using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Zenith;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace ZenithTest
{

	public class UnitTestSelect : IClassFixture<TestsFixture>
	{
		private readonly TestsFixture data;
		public UnitTestSelect(TestsFixture data)
		{
			this.data = data;
		}

		[Fact]
		public void GenSelectSingleTable()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string expect = "SELECT\n\n -- Table `b`\n\t [b].[BossmanId] AS [BossmanId]\n\t,[b].[ContactId] AS [ContactId]\n\t,[b].[Name] AS [Name]\n -- End Table `b`\n\nFROM [Bossman] [b]\n";
				string sql = unitOfWork.CreateSelect<BaseBossman>("b");
				Assert.Equal(expect, sql);
			}
		}

		[Fact]
		public void GenSqlColumnName()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string expect = "SELECT\n\n -- Table `b`\n\t [b].[WorkerId] AS [WorkerId]\n\t,[b].[ContactId] AS [ContactId]\n\t,[b].[Name] AS [Name]\n\t,[b].[UpdatedOn] AS [UpdatedOn]\n -- End Table `b`\n\nFROM [Worker] [b]\n";
				string sql = unitOfWork.CreateSelect<BaseWorker>("b");
				Assert.Equal(expect, sql);
			}
		}

		[Fact]
		public void BasicJoin()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string expect = "SELECT\n\n -- Table `b`\n\t [b].[BossmanId] AS [BossmanId]\n\t,[b].[ContactId] AS [ContactId]\n\t,[b].[Name] AS [Name]\n -- End Table `b`\n\n -- Table `Building`\n\t,[Building].[BossmanBuildingId] AS [Building_BossmanBuildingId]\n\t,[Building].[BossmanId] AS [Building_BossmanId]\n\t,[Building].[BuildingId] AS [Building_BuildingId]\n -- End Table `Building`\n\nFROM [Bossman] [b]\nLEFT JOIN [BossmanBuilding] [Building] ON [b].[BossmanId] = [Building].[BossmanId]\n";
				string sql = unitOfWork.CreateSelect<BossmanEzJoin>("b");
				Assert.Equal(expect, sql);
			}
		}

		[Fact]
		public void MegaJoin()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var unitOfWork = processContainer.GetRequiredService<IUnitOfWork>();
				string expect = "SELECT\n\n -- Table `b`\n\t [b].[BuildingId] AS [BuildingId]\n\t,[b].[Name] AS [Name]\n -- End Table `b`\n\n -- Table `Bossman`\n\t,[Bossman].[BossmanBuildingId] AS [Bossman_BossmanBuildingId]\n\t,[Bossman].[BuildingId] AS [Bossman_BuildingId]\n\t,[Bossman].[BossmanId] AS [Bossman_BossmanId]\n -- End Table `Bossman`\n\n -- Table `Bossman_BossmanMegaJoin`\n\t,[Bossman_BossmanMegaJoin].[BossmanId] AS [Bossman_BossmanMegaJoin_BossmanId]\n\t,[Bossman_BossmanMegaJoin].[ContactId] AS [Bossman_BossmanMegaJoin_ContactId]\n\t,[Bossman_BossmanMegaJoin].[Name] AS [Bossman_BossmanMegaJoin_Name]\n -- End Table `Bossman_BossmanMegaJoin`\n\n -- Table `Bossman_BossmanMegaJoin_Worker`\n\t,[Bossman_BossmanMegaJoin_Worker].[WorkerBossmanId] AS [Bossman_BossmanMegaJoin_Worker_WorkerBossmanId]\n\t,[Bossman_BossmanMegaJoin_Worker].[BossmanId] AS [Bossman_BossmanMegaJoin_Worker_BossmanId]\n\t,[Bossman_BossmanMegaJoin_Worker].[WorkerId] AS [Bossman_BossmanMegaJoin_Worker_WorkerId]\n -- End Table `Bossman_BossmanMegaJoin_Worker`\n\n -- Table `Bossman_BossmanMegaJoin_Worker_Worker`\n\t,[Bossman_BossmanMegaJoin_Worker_Worker].[WorkerId] AS [Bossman_BossmanMegaJoin_Worker_Worker_WorkerId]\n\t,[Bossman_BossmanMegaJoin_Worker_Worker].[ContactId] AS [Bossman_BossmanMegaJoin_Worker_Worker_ContactId]\n\t,[Bossman_BossmanMegaJoin_Worker_Worker].[Name] AS [Bossman_BossmanMegaJoin_Worker_Worker_Name]\n\t,[Bossman_BossmanMegaJoin_Worker_Worker].[UpdatedOn] AS [Bossman_BossmanMegaJoin_Worker_Worker_UpdatedOn]\n -- End Table `Bossman_BossmanMegaJoin_Worker_Worker`\n\n -- Table `Bossman_BossmanMegaJoin_Worker_Worker_Car`\n\t,[Bossman_BossmanMegaJoin_Worker_Worker_Car].[WorkerCarId] AS [Bossman_BossmanMegaJoin_Worker_Worker_Car_WorkerCarId]\n\t,[Bossman_BossmanMegaJoin_Worker_Worker_Car].[WorkerId] AS [Bossman_BossmanMegaJoin_Worker_Worker_Car_WorkerId]\n\t,[Bossman_BossmanMegaJoin_Worker_Worker_Car].[Type] AS [Bossman_BossmanMegaJoin_Worker_Worker_Car_Type]\n -- End Table `Bossman_BossmanMegaJoin_Worker_Worker_Car`\n\n -- Table `Car`\n\t,[Car].[BossmanBuildingId] AS [Car_BossmanBuildingId]\n\t,[Car].[BuildingId] AS [Car_BuildingId]\n\t,[Car].[BossmanId] AS [Car_BossmanId]\n -- End Table `Car`\n\n -- Table `Car_BaseBossman`\n\t,[Car_BaseBossman].[BossmanId] AS [Car_BaseBossman_BossmanId]\n -- End Table `Car_BaseBossman`\n\n -- Table `Car_BaseBossman_WorkerBossman`\n\t,[Car_BaseBossman_WorkerBossman].[WorkerBossmanId] AS [Car_BaseBossman_WorkerBossman_WorkerBossmanId]\n\t,[Car_BaseBossman_WorkerBossman].[BossmanId] AS [Car_BaseBossman_WorkerBossman_BossmanId]\n\t,[Car_BaseBossman_WorkerBossman].[WorkerId] AS [Car_BaseBossman_WorkerBossman_WorkerId]\n -- End Table `Car_BaseBossman_WorkerBossman`\n\n -- Table `Car_BaseBossman_WorkerBossman_BaseWorker`\n\t,[Car_BaseBossman_WorkerBossman_BaseWorker].[WorkerId] AS [Car_BaseBossman_WorkerBossman_BaseWorker_WorkerId]\n -- End Table `Car_BaseBossman_WorkerBossman_BaseWorker`\n\n -- Table `Car_BaseBossman_WorkerBossman_BaseWorker_WorkerCar`\n\t,[Car_BaseBossman_WorkerBossman_BaseWorker_WorkerCar].[WorkerCarId] AS [Car_BaseBossman_WorkerBossman_BaseWorker_WorkerCar_WorkerCarId]\n\t,[Car_BaseBossman_WorkerBossman_BaseWorker_WorkerCar].[WorkerId] AS [Car_BaseBossman_WorkerBossman_BaseWorker_WorkerCar_WorkerId]\n\t,[Car_BaseBossman_WorkerBossman_BaseWorker_WorkerCar].[Type] AS [Car_BaseBossman_WorkerBossman_BaseWorker_WorkerCar_Type]\n -- End Table `Car_BaseBossman_WorkerBossman_BaseWorker_WorkerCar`\n\nFROM [Building] [b]\nLEFT JOIN [BossmanBuilding] [Bossman] ON [b].[BuildingId] = [Bossman].[BuildingId]\nLEFT JOIN [Bossman] [Bossman_BossmanMegaJoin] ON [Bossman].[BossmanId] = [Bossman_BossmanMegaJoin].[BossmanId]\nLEFT JOIN [WorkerBossman] [Bossman_BossmanMegaJoin_Worker] ON [Bossman_BossmanMegaJoin].[BossmanId] = [Bossman_BossmanMegaJoin_Worker].[BossmanId]\nLEFT JOIN [Worker] [Bossman_BossmanMegaJoin_Worker_Worker] ON [Bossman_BossmanMegaJoin_Worker].[WorkerId] = [Bossman_BossmanMegaJoin_Worker_Worker].[WorkerId]\nLEFT JOIN [WorkerCar] [Bossman_BossmanMegaJoin_Worker_Worker_Car] ON [Bossman_BossmanMegaJoin_Worker_Worker].[WorkerId] = [Bossman_BossmanMegaJoin_Worker_Worker_Car].[WorkerId]\nLEFT JOIN [BossmanBuilding] [Car] ON [b].[BuildingId] = [Car].[BuildingId]\nLEFT JOIN [Bossman] [Car_BaseBossman] ON [Car].[BossmanId] = [Car_BaseBossman].[BossmanId]\nLEFT JOIN [WorkerBossman] [Car_BaseBossman_WorkerBossman] ON [Car_BaseBossman].[BossmanId] = [Car_BaseBossman_WorkerBossman].[BossmanId]\nLEFT JOIN [Worker] [Car_BaseBossman_WorkerBossman_BaseWorker] ON [Car_BaseBossman_WorkerBossman].[WorkerId] = [Car_BaseBossman_WorkerBossman_BaseWorker].[WorkerId]\nLEFT JOIN [WorkerCar] [Car_BaseBossman_WorkerBossman_BaseWorker_WorkerCar] ON [Car_BaseBossman_WorkerBossman_BaseWorker].[WorkerId] = [Car_BaseBossman_WorkerBossman_BaseWorker_WorkerCar].[WorkerId] AND ([Car_BaseBossman_WorkerBossman_BaseWorker_WorkerCar].[WorkerId] = 1)\n";
				string sql = unitOfWork.CreateSelect<BuildingMegaJoins>("b");
				Assert.Equal(expect, sql);
			}
		}


	}
}
