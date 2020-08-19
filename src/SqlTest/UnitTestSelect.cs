using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using SqlSharp;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace SqlSharpTest
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
				string expect = "SELECT\n\n -- Table `b`\n\t [b].[BuildingId] AS [BuildingId]\n\t,[b].[Name] AS [Name]\n -- End Table `b`\n\n -- Table `Bossman`\n\t,[Bossman].[BossmanBuildingId] AS [Bossman_BossmanBuildingId]\n\t,[Bossman].[BuildingId] AS [Bossman_BuildingId]\n\t,[Bossman].[BossmanId] AS [Bossman_BossmanId]\n -- End Table `Bossman`\n\n -- Table `Bossman_B`\n\t,[Bossman_B].[BossmanId] AS [Bossman_B_BossmanId]\n\t,[Bossman_B].[ContactId] AS [Bossman_B_ContactId]\n\t,[Bossman_B].[Name] AS [Bossman_B_Name]\n -- End Table `Bossman_B`\n\n -- Table `Bossman_B_Worker`\n\t,[Bossman_B_Worker].[WorkerBossmanId] AS [Bossman_B_Worker_WorkerBossmanId]\n\t,[Bossman_B_Worker].[BossmanId] AS [Bossman_B_Worker_BossmanId]\n\t,[Bossman_B_Worker].[WorkerId] AS [Bossman_B_Worker_WorkerId]\n -- End Table `Bossman_B_Worker`\n\n -- Table `Bossman_B_Worker_W`\n\t,[Bossman_B_Worker_W].[WorkerId] AS [Bossman_B_Worker_W_WorkerId]\n\t,[Bossman_B_Worker_W].[ContactId] AS [Bossman_B_Worker_W_ContactId]\n\t,[Bossman_B_Worker_W].[Name] AS [Bossman_B_Worker_W_Name]\n\t,[Bossman_B_Worker_W].[UpdatedOn] AS [Bossman_B_Worker_W_UpdatedOn]\n -- End Table `Bossman_B_Worker_W`\n\n -- Table `Bossman_B_Worker_W_Car`\n\t,[Bossman_B_Worker_W_Car].[WorkerCarId] AS [Bossman_B_Worker_W_Car_WorkerCarId]\n\t,[Bossman_B_Worker_W_Car].[WorkerId] AS [Bossman_B_Worker_W_Car_WorkerId]\n\t,[Bossman_B_Worker_W_Car].[Type] AS [Bossman_B_Worker_W_Car_Type]\n -- End Table `Bossman_B_Worker_W_Car`\n\n -- Table `Car`\n\t,[Car].[BossmanBuildingId] AS [Car_BossmanBuildingId]\n\t,[Car].[BuildingId] AS [Car_BuildingId]\n\t,[Car].[BossmanId] AS [Car_BossmanId]\n -- End Table `Car`\n\n -- Table `Car_B`\n\t,[Car_B].[BossmanId] AS [Car_B_BossmanId]\n -- End Table `Car_B`\n\n -- Table `Car_B_W`\n\t,[Car_B_W].[WorkerBossmanId] AS [Car_B_W_WorkerBossmanId]\n\t,[Car_B_W].[BossmanId] AS [Car_B_W_BossmanId]\n\t,[Car_B_W].[WorkerId] AS [Car_B_W_WorkerId]\n -- End Table `Car_B_W`\n\n -- Table `Car_B_W_B`\n\t,[Car_B_W_B].[WorkerId] AS [Car_B_W_B_WorkerId]\n -- End Table `Car_B_W_B`\n\n -- Table `Car_B_W_B_W`\n\t,[Car_B_W_B_W].[WorkerCarId] AS [Car_B_W_B_W_WorkerCarId]\n\t,[Car_B_W_B_W].[WorkerId] AS [Car_B_W_B_W_WorkerId]\n\t,[Car_B_W_B_W].[Type] AS [Car_B_W_B_W_Type]\n -- End Table `Car_B_W_B_W`\n\nFROM [Building] [b]\nLEFT JOIN [BossmanBuilding] [Bossman] ON [b].[BuildingId] = [Bossman].[BuildingId]\nLEFT JOIN [Bossman] [Bossman_B] ON [Bossman].[BossmanId] = [Bossman_B].[BossmanId]\nLEFT JOIN [WorkerBossman] [Bossman_B_Worker] ON [Bossman_B].[BossmanId] = [Bossman_B_Worker].[BossmanId]\nLEFT JOIN [Worker] [Bossman_B_Worker_W] ON [Bossman_B_Worker].[WorkerId] = [Bossman_B_Worker_W].[WorkerId]\nLEFT JOIN [WorkerCar] [Bossman_B_Worker_W_Car] ON [Bossman_B_Worker_W].[WorkerId] = [Bossman_B_Worker_W_Car].[WorkerId]\nLEFT JOIN [BossmanBuilding] [Car] ON [b].[BuildingId] = [Car].[BuildingId]\nLEFT JOIN [Bossman] [Car_B] ON [Car].[BossmanId] = [Car_B].[BossmanId]\nLEFT JOIN [WorkerBossman] [Car_B_W] ON [Car_B].[BossmanId] = [Car_B_W].[BossmanId]\nLEFT JOIN [Worker] [Car_B_W_B] ON [Car_B_W].[WorkerId] = [Car_B_W_B].[WorkerId]\nLEFT JOIN [WorkerCar] [Car_B_W_B_W] ON [Car_B_W_B].[WorkerId] = [Car_B_W_B_W].[WorkerId] AND ([Car_B_W_B_W].[WorkerId] = 1)\n";
				string sql = unitOfWork.CreateSelect<BuildingMegaJoins>("b");
				Assert.Equal(expect, sql);
			}
		}


	}
}
